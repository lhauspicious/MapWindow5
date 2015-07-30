﻿// -------------------------------------------------------------------------------------------
// <copyright file="ToolboxGenerator.cs" company="MapWindow OSS Team - www.mapwindow.org">
//  MapWindow OSS Team - 2015
// </copyright>
// -------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MW5.Plugins.Enums;
using MW5.Plugins.Interfaces;
using MW5.Plugins.Services;
using MW5.Shared;
using MW5.Tools.Enums;
using MW5.Tools.Model;
using MW5.Tools.Tools.Database;
using MW5.Tools.Tools.Geoprocessing.VectorGeometryTools;
using MW5.Tools.Tools.Projections;

namespace MW5.Plugins.Toolbox
{
    public class ToolboxGenerator
    {
        private readonly IAppContext _context;

        private readonly ToolboxPlugin _plugin;
        private readonly ILayerService _layerService;

        public ToolboxGenerator(IAppContext context, ToolboxPlugin plugin, ILayerService layerService)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (plugin == null) throw new ArgumentNullException("plugin");
            if (layerService == null) throw new ArgumentNullException("layerService");

            _context = context;
            _plugin = plugin;
            _layerService = layerService;

            Init();
        }

        private void Init()
        {
            GenerateGroups();

            CreateTools();
        }
        
        private void CreateTools()
        {
            var tools = ReflectedTools;

            AddTools(tools);
        }

        private IEnumerable<IGisTool> Tools
        {
            get
            {
                var tools = new List<IGisTool>
                {
                    new IdentifyProjectionTool(),
                    new ImportLayerTool(),
                    new RandomPointsTool()
                };

                return tools;
            }
        }

        /// <summary>
        /// Gets the reflected tools.
        /// </summary>
        /// <value>stackoverflow.com/questions/26733/getting-all-types-that-implement-an-interface</value>
        private IEnumerable<IGisTool> ReflectedTools
        {
            get
            {
                var type = typeof(IGisTool);
                
                // we shall search MW5.Tools
                var list = typeof(GisToolBase).Assembly
                           .GetTypes()
                           .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);

                foreach (var item in list)
                {
                    IGisTool tool = null;

                    try
                    {
                         tool = Activator.CreateInstance(item) as IGisTool;
                    }
                    catch(Exception ex)
                    {
                        Logger.Current.Error("Failed to create GIS tool: {0}.", ex, item.Name);
                    }

                    if (tool != null)
                    {
                        yield return tool;
                    }
                }
            }
        }

        private void AddTools(IEnumerable<IGisTool> tools)
        {
            var groups = _context.Toolbox.Groups;

            foreach (var tool in tools)
            {
                string groupKey = tool.GetType().GetAttributeValue((GisToolAttribute att) => att.GroupKey);

                if (string.IsNullOrWhiteSpace(groupKey))
                {
                    Logger.Current.Warn("No group is specified for the tool: " + tool.Name);
                    continue;
                }

                var group = groups.FindGroup(groupKey);     // can be optimized with dictionary to speed it up

                if (group == null)
                {
                    Logger.Current.Warn("Group with the key wasn't found: " + groupKey);
                    continue;
                }

                group.Tools.Add(tool);
            }
        }

        private void GenerateGroups()
        {
            var groups = _context.Toolbox.Groups;

            var group = groups.Add("Projections", GroupKeys.Projections, _plugin.Identity);
            group.Description = "Tools to work with coorindate systems and projections.";

            group = groups.Add("GeoDatabases", GroupKeys.GeoDatabases, _plugin.Identity);
            group.Description = "Tools to work with spatial datatabases like PostGIS, SpatialLite, MS SQL Spatial, etc.";

            group = groups.Add("Geoprocessing", GroupKeys.Geoprocessing, _plugin.Identity);
            group.Description = "Various geoprocessing operations for vector and raster datasources.";

            var subGroup = group.SubGroups.Add("Vector Geometry Tools", GroupKeys.VectorGeometryTools, _plugin.Identity);
            subGroup.Description = "Geoprocessing tools for vector datasources.";
        }
    }
}