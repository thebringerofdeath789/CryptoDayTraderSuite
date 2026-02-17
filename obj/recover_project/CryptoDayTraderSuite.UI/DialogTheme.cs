using System;
using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite.UI
{
	internal static class DialogTheme
	{
		public static void Apply(Form form)
		{
			if (form == null)
			{
				return;
			}
			form.BackColor = Theme.ContentBg;
			form.ForeColor = Theme.Text;
			form.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
			form.Padding = new Padding(0);
			foreach (Control control in form.Controls)
			{
				ApplyControl(control);
			}
		}

		private static void ApplyControl(Control control)
		{
			if (control == null)
			{
				return;
			}
			if (control is Button)
			{
				Button button = (Button)control;
				button.FlatStyle = FlatStyle.Flat;
				button.FlatAppearance.BorderColor = Theme.PanelBg;
				button.BackColor = Theme.PanelBg;
				button.ForeColor = Theme.Text;
				button.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Regular);
				if (button.Height < 30)
				{
					button.Height = 30;
				}
				if (button.Width < 84)
				{
					button.Width = 84;
				}
			}
			else if (control is TextBox)
			{
				TextBox textBox = (TextBox)control;
				textBox.BorderStyle = BorderStyle.FixedSingle;
				textBox.BackColor = Theme.PanelBg;
				textBox.ForeColor = Theme.Text;
				if (!textBox.Multiline && textBox.Height < 28)
				{
					textBox.Height = 28;
				}
			}
			else if (control is ComboBox)
			{
				ComboBox comboBox = (ComboBox)control;
				comboBox.BackColor = Theme.PanelBg;
				comboBox.ForeColor = Theme.Text;
				comboBox.FlatStyle = FlatStyle.Flat;
				if (comboBox.Height < 28)
				{
					comboBox.Height = 28;
				}
			}
			else if (control is NumericUpDown)
			{
				NumericUpDown numeric = (NumericUpDown)control;
				numeric.BackColor = Theme.PanelBg;
				numeric.ForeColor = Theme.Text;
				if (numeric.Height < 28)
				{
					numeric.Height = 28;
				}
			}
			else if (control is DateTimePicker)
			{
				DateTimePicker picker = (DateTimePicker)control;
				picker.CalendarMonthBackground = Theme.PanelBg;
				picker.CalendarForeColor = Theme.Text;
				picker.CalendarTitleBackColor = Theme.PanelBg;
				picker.CalendarTitleForeColor = Theme.Text;
				picker.CalendarTrailingForeColor = Theme.TextMuted;
				if (picker.Height < 28)
				{
					picker.Height = 28;
				}
			}
			else if (control is CheckBox)
			{
				CheckBox checkBox = (CheckBox)control;
				checkBox.BackColor = Theme.ContentBg;
				checkBox.ForeColor = Theme.Text;
			}
			else if (control is Label)
			{
				Label label = (Label)control;
				label.BackColor = Color.Transparent;
				if (IsSectionHeader(label))
				{
					label.ForeColor = Theme.Accent;
					label.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Regular);
				}
				else
				{
					label.ForeColor = Theme.TextMuted;
					if (label.Font.Bold)
					{
						label.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Regular);
					}
				}
			}
			else if (control is TableLayoutPanel)
			{
				control.BackColor = Theme.ContentBg;
				control.ForeColor = Theme.Text;
			}
			else if (control is FlowLayoutPanel || control is Panel)
			{
				control.BackColor = Theme.ContentBg;
				control.ForeColor = Theme.Text;
				if (control is FlowLayoutPanel flow && flow.FlowDirection == FlowDirection.RightToLeft && flow.Padding.All < 10)
				{
					flow.Padding = new Padding(12, 10, 12, 10);
				}
			}
			else if (control is PropertyGrid)
			{
				PropertyGrid grid = (PropertyGrid)control;
				grid.ViewBackColor = Theme.ContentBg;
				grid.ViewForeColor = Theme.Text;
				grid.LineColor = Theme.PanelBg;
				grid.HelpBackColor = Theme.PanelBg;
				grid.HelpForeColor = Theme.TextMuted;
				grid.CategoryForeColor = Theme.Accent;
				grid.CommandsBackColor = Theme.PanelBg;
				grid.CommandsForeColor = Theme.Text;
			}
			else
			{
				if (control.BackColor == SystemColors.Control)
				{
					control.BackColor = Theme.ContentBg;
				}
				control.ForeColor = Theme.Text;
			}
			foreach (Control child in control.Controls)
			{
				ApplyControl(child);
			}
		}

		private static bool IsSectionHeader(Label label)
		{
			if (label == null || string.IsNullOrWhiteSpace(label.Text))
			{
				return false;
			}
			string text = label.Text.Trim();
			if (text.IndexOf("credential", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			if (text.IndexOf(" fields", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			return false;
		}
	}
}
