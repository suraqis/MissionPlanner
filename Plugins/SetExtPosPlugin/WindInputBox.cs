using System.Drawing;
using System.Windows.Forms;
using MissionPlanner.Controls;

namespace SetExtPosPlugin
{
    public class WindInputBox
    {
        public delegate void ThemeManager(Control ctl);
        public static event ThemeManager ApplyTheme;

        public static string wind_speed = "";
        public static string wind_dir = "";
        public static DialogResult Show(string title, string promptText1, string promptText2, ref string wind_speed, ref string wind_dir)
        {
            DialogResult answer = DialogResult.Cancel;
            WindInputBox.wind_speed = wind_speed;
            WindInputBox.wind_dir = wind_dir;

            // ensure we run this on the right thread - mono - mac
            if (Application.OpenForms.Count > 0 && Application.OpenForms[0].InvokeRequired)
            {
                Application.OpenForms[0].Invoke((MethodInvoker)delegate
                {
                    answer = ShowUI(title, promptText1, promptText2);
                });
            }
            else
            {
                answer = ShowUI(title, promptText1, promptText2);
            }

            wind_speed = WindInputBox.wind_speed;
            wind_dir = WindInputBox.wind_dir;

            return answer;
        }

        static DialogResult ShowUI(string title, string promptText1, string promptText2)
        {
            Form form = new Form();
            Label label1 = new Label();
            Label label2 = new Label();
            TextBox textBoxSpeed = new TextBox();
            TextBox textBoxDirection = new TextBox();
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

            var y = 20;

            // Label for the prompt1 text
            label1.AutoSize = true;

            label1.Size = new Size(372, 13);
            label1.Text = promptText1;
            label1.MaximumSize = new Size(372, 0);

            // TextBox for speed input
            //textBoxSpeed.Location = new Point(label1.Location.X + label1.Width + xMargin, y);
            textBoxSpeed.Size = new Size(50, 20);
            textBoxSpeed.Text = wind_speed;

            // Label for the prompt2 text
            label2.AutoSize = true;
            //label2.Location = new Point(textBoxSpeed.Location.X + len + xMargin, y);
            label2.Size = new Size(372, 13);
            label2.Text = promptText2;
            label2.MaximumSize = new Size(372, 0);


            // TextBox for direction input
            //textBoxSpeed.Location = new Point(label2.Location.X + label2.Width + xMargin, y);
            textBoxDirection.Size = new Size(50, 20);
            textBoxDirection.Text = wind_dir;

            // OK Button
            buttonOk.Size = new Size(75, 23);
            buttonOk.Text = "OK";
            buttonOk.DialogResult = DialogResult.OK;

            // Cancel Button
            buttonCancel.Size = new Size(75, 23);
            buttonCancel.Text = "Cancel";
            buttonCancel.DialogResult = DialogResult.Cancel;

            // Add controls to the form
            form.Controls.Add(label1);
            form.Controls.Add(textBoxSpeed);
            form.Controls.Add(label2);
            form.Controls.Add(textBoxDirection);
            form.Controls.Add(buttonOk);
            form.Controls.Add(buttonCancel);

            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            // Resume layout
            form.ResumeLayout(false);
            form.PerformLayout();

            // Adjust the location of textBox, buttonOk, buttonCancel based on the content of the label.
            //y = y + label1.Height + yMargin;
            label1.Location = new Point(xMargin, y);
            textBoxSpeed.Location = new Point(label1.Location.X + label1.Width + xMargin, y);
            label2.Location = new Point(textBoxSpeed.Location.X + textBoxSpeed.Width + xMargin, y);
            textBoxDirection.Location = new Point(label2.Location.X + label2.Width + xMargin, y);
            y = y + textBoxSpeed.Height + yMargin;

            // Increase the size of the form.
            form.ClientSize = new Size(label1.Width + textBoxSpeed.Width + label2.Width + textBoxDirection.Width + 5*xMargin, y + buttonOk.Height + yMargin);
            buttonCancel.Location = new Point(form.ClientSize.Width - buttonCancel.Width - 2 * xMargin, y);
            buttonOk.Location = new Point(buttonCancel.Location.X - buttonOk.Width - 2 * xMargin, y);

            // Apply any theme settings
            ApplyTheme?.Invoke(form);

            // Show the form and return the result
            DialogResult dialogResult = form.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                wind_speed = textBoxSpeed.Text;
                wind_dir = textBoxDirection.Text;
            }

            form.Dispose();
            return dialogResult;
        }
    }
}
