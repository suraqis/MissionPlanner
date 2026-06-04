using System.Drawing;
using System.Windows.Forms;
using MissionPlanner.Controls;

namespace SetExtPosPlugin
{
    public class SetPositionBox
    {
        public delegate void ThemeManager(Control ctl);
        public static event ThemeManager ApplyTheme;

        public static DialogResult Show(string title, string promptText)
        {
            DialogResult answer = DialogResult.Cancel;

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

            return answer;
        }

        static DialogResult ShowUI(string title, string promptText)
        {
            Form form = new Form();
            Label label = new Label();
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
            label.AutoSize = true;

            label.Size = new Size(372, 13);
            label.Text = promptText;
            label.MaximumSize = new Size(372, 0);

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
            form.Controls.Add(buttonOk);
            form.Controls.Add(buttonCancel);

            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            // Resume layout
            form.ResumeLayout(false);
            form.PerformLayout();

            // Adjust the location of textBox, buttonOk, buttonCancel based on the content of the label.
            //y = y + label1.Height + yMargin;
            label.Location = new Point(xMargin, y);

            y = y + label.Height + yMargin;

            // Increase the size of the form.
            form.ClientSize = new Size(label.Width + 2 * xMargin, y + buttonOk.Height + yMargin);
            buttonOk.Location = new Point(form.ClientSize.Width / 2 - buttonOk.Width - xMargin, y);
            buttonCancel.Location = new Point(form.ClientSize.Width / 2 + xMargin, y);

            // Apply any theme settings
            ApplyTheme?.Invoke(form);

            // Show the form and return the result
            DialogResult dialogResult = form.ShowDialog();

            form.Dispose();
            return dialogResult;
        }
    }
}

