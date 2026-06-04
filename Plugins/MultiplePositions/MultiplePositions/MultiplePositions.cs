using GMap.NET;
using GMap.NET.WindowsForms;
using MissionPlanner.GCSViews;
using MissionPlanner.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MissionPlanner.plugins
{
    public class MultiplePositions : Plugin.Plugin
    {
        public override string Name { get; } = "Multiple UAV positions";

        public override string Version { get; } = "0.4";

        public override string Author { get; } = "Alex Chen";

        public override bool Exit()
        {
            return true;
        }

        private GMapOverlay overlay;
        private ToolStripMenuItem rootbut;
        private Dictionary<String, bool> displayItems;

        public override bool Init()
        {
            return true;
        }

        public override bool Loaded()
        {
            rootbut = new ToolStripMenuItem("[A] Show multiple positions");
            ToolStripItemCollection col = Host.FDMenuMap.Items;
            col.Add(rootbut);
            displayItems = new Dictionary<string, bool>()
            {
                {"GPS1", false},
                {"GPS2", false},
                {"AHRS2", false},
                //{"AHRS3", false},
                //{"HighLatency", false},
                //{"HighLatency2", false},
                {"SimState", false},
            };
            foreach (var item in displayItems)
            {
                var but = new ToolStripMenuItem(item.Key);
                var key = item.Key;
                but.CheckOnClick = true;
                but.Checked = item.Value;
                but.Click += (s, e) =>
                {
                    var clickedButton = (ToolStripMenuItem)s;  
                    displayItems[key] = clickedButton.Checked;
                };
                rootbut.DropDownItems.Add(but);
            }

            overlay = new GMapOverlay("positions");
            FlightData.instance.gMapControl1.Overlays.Insert(0, overlay);

            MainV2.comPort.OnPacketReceived -= OnComPortOnOnPacketReceived;
            MainV2.comPort.OnPacketReceived += OnComPortOnOnPacketReceived;

            return true;
        }

        private void OnComPortOnOnPacketReceived(object sender, MAVLink.MAVLinkMessage message)
        {
            var ID = message.sysid * 256 + message.compid;
            switch ((MAVLink.MAVLINK_MSG_ID)message.msgid)
            {
                case MAVLink.MAVLINK_MSG_ID.GPS_RAW_INT:
                    {
                        var pos = (MAVLink.mavlink_gps_raw_int_t)message.data;
                        UpdateOrCreate(new PointLatLng(pos.lat / 1e7, pos.lon / 1e7), pos.cog/100, float.NaN, float.NaN, float.NaN, ID, "GPS1", 2);
                        break;
                    }
                case MAVLink.MAVLINK_MSG_ID.GPS2_RAW:
                    {
                        var pos = (MAVLink.mavlink_gps2_raw_t)message.data;
                        UpdateOrCreate(new PointLatLng(pos.lat / 1e7, pos.lon / 1e7), pos.cog/100, float.NaN, float.NaN, float.NaN, ID, "GPS2", 3);
                        break;
                    }
                case MAVLink.MAVLINK_MSG_ID.AHRS2:
                    {
                        var pos = (MAVLink.mavlink_ahrs2_t)message.data;
                        UpdateOrCreate(new PointLatLng(pos.lat / 1e7, pos.lng / 1e7), pos.yaw * 180 / 3.1415f, float.NaN, float.NaN, float.NaN, ID, "AHRS2", 5);
                        break;
                    }
                //case MAVLink.MAVLINK_MSG_ID.AHRS3:
                //{
                //    var pos = (MAVLink.mavlink_ahrs3_t)message.data;
                //    UpdateOrCreate(new PointLatLng(pos.lat / 1e7, pos.lng / 1e7), pos.yaw, ID, "AHRS3");
                //    break;
                //}
                //case MAVLink.MAVLINK_MSG_ID.HIGH_LATENCY:
                //{
                //    var pos = (MAVLink.mavlink_high_latency_t)message.data;
                //    UpdateOrCreate(new PointLatLng(pos.latitude / 1e7, pos.longitude / 1e7), ID, "HighLatency");
                //    break;
                //}
                //case MAVLink.MAVLINK_MSG_ID.HIGH_LATENCY2:
                //{
                //    var pos = (MAVLink.mavlink_high_latency2_t)message.data;
                //    UpdateOrCreate(new PointLatLng(pos.latitude / 1e7, pos.longitude / 1e7), ID, "HighLatency2");
                //    break;
                //}
                case MAVLink.MAVLINK_MSG_ID.SIMSTATE:
                {
                    var pos = (MAVLink.mavlink_simstate_t)message.data;
                    UpdateOrCreate(new PointLatLng(pos.lat / 1e7, pos.lng / 1e7), pos.yaw * 180 / 3.1415f, float.NaN, float.NaN, float.NaN, ID, "SimState", 1);
                    break;
                }
                //case MAVLink.MAVLINK_MSG_ID.SIM_STATE:
                //{
                //    var pos = (MAVLink.mavlink_sim_state_t)message.data;
                //    UpdateOrCreate(new PointLatLng(pos.lat, pos.lon), ID, "Sim State");
                //    break;
                //}
                //case MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT:
                //{
                //    var pos = (MAVLink.mavlink_global_position_int_t)message.data;
                //    UpdateOrCreate(new PointLatLng(pos.lat / 1e7, pos.lon / 1e7), pos.hdg, ID, "Global Position");
                //    break;
                //}

            }
        }

        private void UpdateOrCreate(PointLatLng pointLatLng, float heading, float cog, float nav_bearing, float target, int ID, string sourcetext = "", int color = 1)
        {
            var existing = overlay.Markers.Where(a => a.Tag.ToString() == ID.ToString() + sourcetext);
            if (existing.Count() > 0)
            {
                var marker = (GMapMarkerPlane)existing.First();
                marker.Position = pointLatLng;
                marker.Heading = heading;
                marker.Cog = cog;
                marker.Target = nav_bearing;
                marker.Nav_bearing = target;
                ((GMapMarkerPlane_custom)marker).LastUpdate = DateTime.Now;
                marker.IsVisible = displayItems[sourcetext];
            }
            else
            {
                var marker = new GMapMarkerPlane_custom(pointLatLng, heading, cog, nav_bearing, target, color)
                { Tag = ID.ToString() + sourcetext, ToolTipText = sourcetext, ToolTipMode = MarkerTooltipMode.OnMouseOver, LastUpdate = DateTime.Now };
                overlay.Markers.Add(marker);
            }
        }

        public override bool Loop()
        {
            try
            {
                var cutoff = DateTime.Now.AddSeconds(-10);

                var stale = overlay.Markers
                    .OfType<GMapMarkerPlane_custom>()
                    .Where(m => m.LastUpdate < cutoff)
                    .ToList();

                foreach (var marker in stale)
                {
                    overlay.Markers.Remove(marker);
                }
            }
            catch (Exception e)
            {
            }

            return true;
        }
    }

    public class GMapMarkerPlane_custom : GMapMarkerPlane
    {
        public DateTime LastUpdate = DateTime.MinValue;
        
        public GMapMarkerPlane_custom(PointLatLng p, float heading, float cog, float nav_bearing, float target, int color) 
            : base(color, p, heading, cog, nav_bearing, target, -1)
        {
            GMapMarkerPlane_custom.DisplayHeadingSetting = false;
        }

    }

}
