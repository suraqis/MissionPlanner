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

    public class AltInputBox
    {
        public delegate void ThemeManager(Control ctl);
        public static event ThemeManager ApplyTheme;

        public static string alt = "";
        public static MAVLink.MAV_FRAME frame = MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT; // Holds the altitude frame selection

        public static DialogResult Show(string title, string promptText, ref string alt, ref MAVLink.MAV_FRAME frame)
        {
            DialogResult answer = DialogResult.Cancel;
            AltInputBox.alt = alt;
            AltInputBox.frame = frame;

            // ensure we run this on the right thread - mono - mac
            if (Application.OpenForms.Count > 0 && Application.OpenForms[0].InvokeRequired)
            {
                Application.OpenForms[0].Invoke((MethodInvoker)delegate
                {
                    answer = ShowUI(title, promptText);
                });
            }
            else
            {
                answer = ShowUI(title, promptText);
            }

            alt = AltInputBox.alt;
            frame = AltInputBox.frame;

            return answer;
        }

        static DialogResult ShowUI(string title, string promptText)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBoxAlt = new TextBox();
            ComboBox comboBoxFrame = new ComboBox();
            MyButton buttonOk = new MyButton();
            MyButton buttonCancel = new MyButton();

            // Form layout setup
            form.SuspendLayout();
            const int yMargin = 10;
            const int xMargin = 10;

            form.TopMost = true;
            form.TopLevel = true;
            form.Text = title;
            form.ClientSize = new Size(396, 150); // Keep the same form width
            form.FormBorderStyle = FormBorderStyle.FixedSingle;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;

            // Label for the prompt text
            var y = 20;
            label.AutoSize = true;
            label.Location = new Point(9, y);
            label.Size = new Size(372, 13);
            label.Text = promptText;
            label.MaximumSize = new Size(372, 0);

            // ComboBox for altitude frame selection (reduce width, aligned next to TextBox)
            comboBoxFrame.Size = new Size(70, 20);
            comboBoxFrame.DropDownStyle = ComboBoxStyle.DropDownList;

            // Add KeyValuePair items for each option
            comboBoxFrame.Items.Add(new KeyValuePair<string, MAVLink.MAV_FRAME>("Absolute", MAVLink.MAV_FRAME.GLOBAL));
            comboBoxFrame.Items.Add(new KeyValuePair<string, MAVLink.MAV_FRAME>("Relative", MAVLink.MAV_FRAME.GLOBAL_RELATIVE_ALT));
            comboBoxFrame.Items.Add(new KeyValuePair<string, MAVLink.MAV_FRAME>("Terrain", MAVLink.MAV_FRAME.GLOBAL_TERRAIN_ALT));
            comboBoxFrame.DisplayMember = "Key";
            comboBoxFrame.ValueMember = "Value";

            // Set the initial selected value by matching the frameValue
            comboBoxFrame.SelectedIndex = comboBoxFrame.Items.Cast<KeyValuePair<string, MAVLink.MAV_FRAME>>()
                .ToList().FindIndex(item => item.Value == frame);

            // TextBox for altitude input
            textBoxAlt.Size = new Size(form.ClientSize.Width - comboBoxFrame.Width - 3 * xMargin, 20);
            textBoxAlt.Text = alt;

            // OK Button
            buttonOk.Size = new Size(75, 23);
            buttonOk.Text = "OK";
            buttonOk.DialogResult = DialogResult.OK;

            // Cancel Button
            buttonCancel.Size = new Size(75, 23);
            buttonCancel.Text = "Cancel";
            buttonCancel.DialogResult = DialogResult.Cancel;

            // Add controls to the form
            form.Controls.Add(label);
            form.Controls.Add(textBoxAlt);
            form.Controls.Add(comboBoxFrame);
            form.Controls.Add(buttonOk);
            form.Controls.Add(buttonCancel);

            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            // Resume layout
            form.ResumeLayout(false);
            form.PerformLayout();

            // Adjust the location of textBox, buttonOk, buttonCancel based on the content of the label.
            y = y + label.Height + yMargin;
            comboBoxFrame.Location = new Point(xMargin, y);
            textBoxAlt.Location = new Point(comboBoxFrame.Location.X + comboBoxFrame.Width + xMargin, y + 4);
            y = y + textBoxAlt.Height + yMargin;
            buttonOk.Location = new Point(228, y);
            buttonCancel.Location = new Point(309, y);
            // Increase the size of the form.
            form.ClientSize = new Size(396, y + buttonOk.Height + yMargin);

            // Apply any theme settings
            ApplyTheme?.Invoke(form);

            // Show the form and return the result
            DialogResult dialogResult = form.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                alt = textBoxAlt.Text;
                frame = (MAVLink.MAV_FRAME)((dynamic)comboBoxFrame.SelectedItem).Value;
            }

            form.Dispose();
            return dialogResult;
        }
    }

}