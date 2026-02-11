using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using MissionPlanner.GCSViews;
using System.Drawing;
using System.Runtime.Serialization;
using MissionPlanner.Maps;

//TODO: Set overlay order below main UAV marker 
//TODO: Make switch for every position type
namespace MissionPlanner.plugins
{
    public class MultiplePositions : Plugin.Plugin
    {
        public override string Name { get; } = "Multiple UAV positions";

        public override string Version { get; } = "0.1";

        public override string Author { get; } = "Alex Chen";

        public override bool Exit()
        {
            return true;
        }

        private GMapOverlay overlay;
        private ToolStripMenuItem rootbut;
        private bool active = false;

        public override bool Init()
        {
            rootbut = new ToolStripMenuItem("Show multiple positions");
            rootbut.Click += but_Click;
            ToolStripItemCollection col = Host.FDMenuMap.Items;
            col.Add(rootbut);

            overlay = new GMapOverlay("positions");
            FlightData.instance.gMapControl1.Overlays.Insert(0, overlay);

            return true;
        }

        private void but_Click(object sender2, EventArgs e)
        {
            loopratehz = 1;
            active = !active;

            if (active)
            {
                rootbut.Text = "Hide multiple positions";
                overlay.IsVisibile = true;
                // this needs to be set per "comport" - prevent any duplicates
                MainV2.comPort.OnPacketReceived -= OnComPortOnOnPacketReceived;
                MainV2.comPort.OnPacketReceived += OnComPortOnOnPacketReceived;

            }
            else
            {
                MainV2.comPort.OnPacketReceived -= OnComPortOnOnPacketReceived;
                overlay.IsVisibile = false;
                rootbut.Text = "Show multiple positions";
            }
        }

        private void OnComPortOnOnPacketReceived(object sender, MAVLink.MAVLinkMessage message)
        {
            var ID = message.sysid * 256 + message.compid;
            switch ((MAVLink.MAVLINK_MSG_ID)message.msgid)
            {
                case MAVLink.MAVLINK_MSG_ID.GPS_RAW_INT:
                    {
                        var pos = (MAVLink.mavlink_gps_raw_int_t)message.data;
                        UpdateOrCreate(new PointLatLng(pos.lat / 1e7, pos.lon / 1e7), Host.cs.yaw, Host.cs.groundcourse, Host.cs.nav_bearing, Host.cs.target_bearing, ID, "GPS1", 2);
                        break;
                    }
                case MAVLink.MAVLINK_MSG_ID.GPS2_RAW:
                    {
                        var pos = (MAVLink.mavlink_gps2_raw_t)message.data;
                        UpdateOrCreate(new PointLatLng(pos.lat / 1e7, pos.lon / 1e7), Host.cs.yaw, Host.cs.groundcourse2, Host.cs.nav_bearing, Host.cs.target_bearing, ID, "GPS2", 2);
                        break;
                    }
                case MAVLink.MAVLINK_MSG_ID.AHRS2:
                    {
                        var pos = (MAVLink.mavlink_ahrs2_t)message.data;
                        UpdateOrCreate(new PointLatLng(pos.lat / 1e7, pos.lng / 1e7), pos.yaw * 180 / 3.1415f, pos.yaw * 180 / 3.1415f, Host.cs.nav_bearing, Host.cs.target_bearing, ID, "AHRS2", 5);
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
                    UpdateOrCreate(new PointLatLng(pos.lat / 1e7, pos.lng / 1e7), pos.yaw * 180 / 3.1415f, pos.yaw * 180 / 3.1415f, Host.cs.nav_bearing, Host.cs.target_bearing, ID, "SimState", 1);
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
            if (active)
            {
                overlay.IsVisibile = true;
            }
            ;
        }

        private void UpdateOrCreate(PointLatLng pointLatLng, float heading, float cog, float nav_bearing, float target, int ID, string sourcetext = "", int color = 1)
        {
            var existing = overlay.Markers.Where(a => a.Tag.ToString() == ID.ToString() + sourcetext);
            if (existing.Count() > 0)
            {
                existing.First().Position = pointLatLng;
                ((GMapMarkerPlane)existing.First()).Heading = heading;
                ((GMapMarkerPlane)existing.First()).Cog = cog;
                ((GMapMarkerPlane)existing.First()).Target = nav_bearing;
                ((GMapMarkerPlane)existing.First()).Nav_bearing = target;
                ((GMapMarkerPlane_edit)existing.First()).LastUpdate = DateTime.Now;
            }
            else
            {
                var marker = new GMapMarkerPlane_edit(pointLatLng, heading, cog, nav_bearing, target, color)
                { Tag = ID.ToString() + sourcetext, ToolTipText = sourcetext, ToolTipMode = MarkerTooltipMode.OnMouseOver, LastUpdate = DateTime.Now };
                //int index = overlay.Markers.Count;
                overlay.Markers.Add(marker);

            }
        }

        public override bool Loaded()
        {
            return true;
        }

        public override bool Loop()
        {
            try
            {

            }
            catch (Exception e)
            {

            }

            return true;
        }
    }

    public class GMapMarkerPlane_edit : GMapMarkerPlane
    {
        public DateTime LastUpdate = DateTime.MinValue;
        public GMapMarkerPlane_edit(PointLatLng p, float heading, float cog, float nav_bearing, float target, int color) : base(color, p, heading, cog, nav_bearing, target, -1)
        {

        }

    }
}
