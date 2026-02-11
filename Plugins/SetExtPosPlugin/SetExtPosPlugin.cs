
using System;
using System.Windows.Forms;
using GMap.NET;
using SetExtPosPlugin;
using static MAVLink;
using System.Threading.Tasks;
using MissionPlanner.Utilities;

namespace MissionPlanner.plugins
{
    public class setextpos : Plugin.Plugin
    {
        public override string Name { get; } = "Set external position";
        public override string Version { get; } = "0.3";
        public override string Author { get; } = "Alex Chen";

        public override bool Exit()
        {
            return true;
        }

        private ToolStripMenuItem iamherebut;
        private ToolStripMenuItem setwindbut;
        private ToolStripMenuItem fakegpsbut;

        private PointLatLng MouseDownStart;

        public override bool Init()
        {
            iamherebut = new ToolStripMenuItem("[A] I am here");
            iamherebut.Click += setposbut_Click;

            setwindbut = new ToolStripMenuItem("[A] Set wind estimate");
            setwindbut.Click += setwindbut_Click;

            fakegpsbut = new ToolStripMenuItem("[A] Set no-GPS takeoff position");
            fakegpsbut.Click += fakegpsbut_Click;

            ToolStripItemCollection col = Host.FDMenuMap.Items;

            col.Add(iamherebut);
            col.Add(setwindbut);
            col.Add(fakegpsbut);

            return true;
        }

        private void setposbut_Click(object sender2, EventArgs e)
        {

            if (DialogResult.Cancel == SetPositionBox.Show("Select position on map", "Click on map to set current vehicle position"))
                return;

            // add click on map handler here
            Host.FDGMapControl.MouseDown -= setposmap_Click;
            Host.FDGMapControl.MouseDown += setposmap_Click;

        }
        private void setposmap_Click(object sender2, EventArgs e)
        {

            if (MainV2.comPort.BaseStream.IsOpen)
            {
                MouseDownStart = Host.FDMenuMapPosition;
                try
                {
                    Host.comPort.doCommandInt((byte)MainV2.comPort.sysidcurrent,
                        (byte)MainV2.comPort.compidcurrent,
                        MAVLink.MAV_CMD.EXTERNAL_POSITION_ESTIMATE, 0, 0, 0, 0, (int)(MouseDownStart.Lat * 1e7),
                        (int)(MouseDownStart.Lng * 1e7), float.NaN, frame: MAVLink.MAV_FRAME.GLOBAL);

                }
                catch
                {
                    CustomMessageBox.Show(Strings.CommandFailed, Strings.ERROR);
                }
            }
            Host.FDGMapControl.MouseDown -= setposmap_Click;
        }
        private void setwindbut_Click(object sender2, EventArgs e)
        {
            string vel_str = Host.cs.wind_vel.ToString("0.0");
            string dir_str = Host.cs.wind_dir.ToString("0");
            float wind_vel = 0.0f;
            float wind_dir = 0.0f;
            float vel_acc = 0.5f;
            float dir_acc = 1.0f;

            if (DialogResult.Cancel == WindInputBox.Show("Enter Wind Estimate", "Wind Speed:", "Wind Direction:", ref vel_str, ref dir_str))
                return;
            if (vel_str == "") vel_str = "0";
            if (dir_str == "") dir_str = "0";

            if (!float.TryParse(vel_str, out wind_vel) || !float.TryParse(dir_str, out wind_dir))
            {
                CustomMessageBox.Show("Bad wind speed or direction");
                return;
            }

            if (MainV2.comPort.BaseStream.IsOpen)
            {
                try
                {
                    Host.comPort.doCommandInt((byte)MainV2.comPort.sysidcurrent,
                        (byte)MainV2.comPort.compidcurrent,
                        MAVLink.MAV_CMD.EXTERNAL_WIND_ESTIMATE, wind_vel, vel_acc, wind_dir, dir_acc, 0, 0, 0);
                }
                catch
                {
                    CustomMessageBox.Show(Strings.CommandFailed, Strings.ERROR);
                }
            }

        }

        private void fakegpsbut_Click(object sender2, EventArgs e)
        {

            if (DialogResult.Cancel == SetPositionBox.Show("Select position on map", "Click on map to set no-GPS takeoff position"))
                return;

            // add click on map handler here
            Host.FDGMapControl.MouseDown -= fakegpsmap_Click;
            Host.FDGMapControl.MouseDown += fakegpsmap_Click;

        }
        private async void fakegpsmap_Click(object sender2, EventArgs e)
        {
            PointLatLngAlt location = new PointLatLngAlt();
            location.Lat = Host.FDMenuMapPosition.Lat;
            location.Lng = Host.FDMenuMapPosition.Lng;
            location.Alt = srtm.getAltitude(location.Lat, location.Lng).alt;

            await SendGpsInput(location);

            Host.FDGMapControl.MouseDown -= fakegpsmap_Click;
        }

        private async Task SendGpsInput(PointLatLngAlt point)
        {
            var port = MainV2.comPort;

            var gpsMsg = new MAVLink.mavlink_gps_input_t
            {
                gps_id = 1,
                time_usec = (ulong)(DateTime.Now.Millisecond * 1000),
                ignore_flags = (ushort)(
                    GPS_INPUT_IGNORE_FLAGS.GPS_INPUT_IGNORE_FLAG_HDOP |
                    GPS_INPUT_IGNORE_FLAGS.GPS_INPUT_IGNORE_FLAG_VDOP |
                    GPS_INPUT_IGNORE_FLAGS.GPS_INPUT_IGNORE_FLAG_VEL_HORIZ |
                    GPS_INPUT_IGNORE_FLAGS.GPS_INPUT_IGNORE_FLAG_VEL_VERT |
                    GPS_INPUT_IGNORE_FLAGS.GPS_INPUT_IGNORE_FLAG_SPEED_ACCURACY |
                    GPS_INPUT_IGNORE_FLAGS.GPS_INPUT_IGNORE_FLAG_HORIZONTAL_ACCURACY |
                    GPS_INPUT_IGNORE_FLAGS.GPS_INPUT_IGNORE_FLAG_VERTICAL_ACCURACY
                ),
                lat = (int)(point.Lat * 1e7),
                lon = (int)(point.Lng * 1e7),
                alt = (int)point.Alt, 
                hdop = 0,
                vdop = 0,
                vn = 0,
                ve = 0,
                vd = 0,
                speed_accuracy = 0,
                horiz_accuracy = 0,
                vert_accuracy = 0,
                fix_type = 3,
                satellites_visible = 30,
                yaw = (ushort)Host.cs.yaw,
                time_week_ms = 0,
            };

            if (/* Host.cs.armed == false && */ Host.cs.gpsstatus2 <= (float)GPS_FIX_TYPE.NO_FIX && Host.cs.airspeed < 5.0)
            {
                fakegpsbut.Enabled = false;
                for (int i = 0; i < 100; i++)
                {
                    gpsMsg.time_usec = (ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds * 1000;
                    GetGpsWeekAndWeekMs(DateTime.Now, out gpsMsg.time_week, out gpsMsg.time_week_ms);

                    port.sendPacket(gpsMsg, port.MAV.sysid, port.MAV.compid);

                    await Task.Delay(200);
                }
                //Host.comPort.doCommandInt(port.MAV.sysid, port.MAV.compid,
                //    MAVLink.MAV_CMD.DO_SET_HOME, 0, 0, 0, 0, (int)(point.Lat * 1e7),
                //    (int)(point.Lng * 1e7), (float)point.Alt);

                //await Task.Delay(100);

                Host.comPort.doCommandInt((byte)MainV2.comPort.sysidcurrent,
                        (byte)MainV2.comPort.compidcurrent,
                        MAVLink.MAV_CMD.EXTERNAL_WIND_ESTIMATE, 0.0f, 0.5f, 0.0f, 1.0f, 0, 0, 0);
                fakegpsbut.Enabled = true;
            }
        }
        public static void GetGpsWeekAndWeekMs(DateTime now, out ushort gpsWeek, out uint gpsWeekMs)
        {
            // GPS epoch: January 6, 1980
            DateTime gpsEpoch = new DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc);

            // Convert 'now' to UTC if it's not already
            var utcNow = now.ToUniversalTime();

            // Time since GPS epoch
            TimeSpan span = utcNow - gpsEpoch;

            gpsWeek = (ushort)(span.Days / 7);
            gpsWeekMs = (uint)((span.TotalMilliseconds) % (7 * 24 * 60 * 60 * 1000));
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

}