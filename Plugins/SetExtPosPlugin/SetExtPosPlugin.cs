using MissionPlanner.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GMap.NET;
using System.Drawing;
using SetExtPosPlugin;

namespace MissionPlanner.plugins
{
    public class setextpos : Plugin.Plugin
    {
        public override string Name { get; } = "Set external position";
        public override string Version { get; } = "0.2";
        public override string Author { get; } = "Alex Chen";

        public override bool Exit()
        {
            return true;
        }

        private ToolStripMenuItem setposbut;
        private ToolStripMenuItem setwindbut;
        private PointLatLng MouseDownStart;

        public override bool Init()
        {
            setposbut = new ToolStripMenuItem("Set current position");
            setposbut.Click += setposbut_Click;

            setwindbut = new ToolStripMenuItem("Set wind estimate");
            setwindbut.Click += setwindbut_Click;

            ToolStripItemCollection col = Host.FDMenuMap.Items;

            col.Add(setposbut);
            col.Add(setwindbut);

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
            float dir_acc = 1f;

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