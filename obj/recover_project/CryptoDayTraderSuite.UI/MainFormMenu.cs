using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using CryptoDayTraderSuite.Exchanges;

namespace CryptoDayTraderSuite.UI
{
	public static class MainFormMenu
	{
		public static void Attach(MainForm f)
		{
			MenuStrip menu = new MenuStrip();
			menu.Dock = DockStyle.Top;
			f.MainMenuStrip = menu;
			f.Controls.Add(menu);
			f.Controls.SetChildIndex(menu, 0);
			ToolStripMenuItem mFile = new ToolStripMenuItem("File");
			ToolStripMenuItem mProfiles = new ToolStripMenuItem("Profiles");
			ToolStripMenuItem mAccounts = new ToolStripMenuItem("Accounts");
			ToolStripMenuItem mKeys = new ToolStripMenuItem("API Keys");
			ToolStripMenuItem mData = new ToolStripMenuItem("Data");
			ToolStripMenuItem mTrading = new ToolStripMenuItem("Trading");
			ToolStripMenuItem mView = new ToolStripMenuItem("View");
			ToolStripMenuItem mTools = new ToolStripMenuItem("Tools");
			ToolStripMenuItem mLogs = new ToolStripMenuItem("Logs");
			ToolStripMenuItem mHelp = new ToolStripMenuItem("Help");
			ToolStripMenuItem mPlanner = new ToolStripMenuItem("Planner");
			menu.Items.AddRange(new ToolStripItem[11]
			{
				mFile, mProfiles, mAccounts, mKeys, mData, mTrading, mView, mTools, mLogs, mHelp,
				mPlanner
			});
			mFile.DropDownItems.Add(new ToolStripMenuItem("Exit", null, delegate
			{
				f.Close();
			}));
			mProfiles.DropDownItems.Add(new ToolStripMenuItem("Export Profile...", null, delegate
			{
				ExportProfile(f);
			}));
			mProfiles.DropDownItems.Add(new ToolStripMenuItem("Import Profile...", null, delegate
			{
				ImportProfile(f);
			}));
			mAccounts.DropDownItems.Add(new ToolStripMenuItem("Manage Accounts...", null, delegate
			{
				using (AccountsForm accountsForm = new AccountsForm(f.AccountService))
				{
					accountsForm.ShowDialog(f);
				}
			}));
			mKeys.DropDownItems.Add(new ToolStripMenuItem("Manage API Keys...", null, delegate
			{
				using (KeysForm keysForm = new KeysForm(f.KeyService))
				{
					keysForm.ShowDialog(f);
				}
			}));
			mData.DropDownItems.Add(new ToolStripMenuItem("Load Products", null, delegate
			{
				TryClick(f, "btnLoadProducts");
			}));
			mData.DropDownItems.Add(new ToolStripMenuItem("Get Fees", null, delegate
			{
				TryClick(f, "btnFees");
			}));
			mTrading.DropDownItems.Add(new ToolStripMenuItem("Auto Mode...", null, delegate
			{
				IExchangeClient client = f.ExchangeProvider.CreatePublicClient("Coinbase");
				using (AutoModeForm autoModeForm = new AutoModeForm(f.AutoPlanner, client, f.KeyService, f.AccountService))
				{
					autoModeForm.ShowDialog(f);
				}
			}));
			mTrading.DropDownItems.Add(new ToolStripMenuItem("Status / PnL vs Projection...", null, delegate
			{
				using (StatusForm statusForm = new StatusForm(f.HistoryService))
				{
					statusForm.ShowDialog(f);
				}
			}));
			mTrading.DropDownItems.Add(new ToolStripMenuItem("Settings...", null, delegate
			{
				using (SettingsForm settingsForm = new SettingsForm())
				{
					settingsForm.ShowDialog(f);
				}
			}));
			mView.DropDownItems.Add(new ToolStripMenuItem("Refresh Layout", null, delegate
			{
				f.PerformLayout();
			}));
			mTools.DropDownItems.Add(new ToolStripMenuItem("Backtest", null, delegate
			{
				TryClick(f, "btnBacktest");
			}));
			mTools.DropDownItems.Add(new ToolStripMenuItem("Paper Trade", null, delegate
			{
				TryClick(f, "btnPaper");
			}));
			mTools.DropDownItems.Add(new ToolStripMenuItem("Live Trade", null, delegate
			{
				TryClick(f, "btnLive");
			}));
			mTools.DropDownItems.Add(new ToolStripMenuItem("Planner...", null, delegate
			{
				f.OpenPlanner();
			}));
			mLogs.DropDownItems.Add(new ToolStripMenuItem("Open Log Folder", null, delegate
			{
				OpenLogFolder();
			}));
			mLogs.DropDownItems.Add(new ToolStripMenuItem("Copy Recent Log To Clipboard", null, delegate
			{
				CopyRecentLogToClipboard(f);
			}));
			mHelp.DropDownItems.Add(new ToolStripMenuItem("Open README", null, delegate
			{
				OpenReadme();
			}));
			mHelp.DropDownItems.Add(new ToolStripMenuItem("About...", null, delegate
			{
				using (AboutForm aboutForm = new AboutForm())
				{
					aboutForm.ShowDialog(f);
				}
			}));
			mPlanner.DropDownItems.Add(new ToolStripMenuItem("Open Planner...", null, delegate
			{
				f.OpenPlanner();
			}));
		}

		private static void TryClick(Form f, string fieldName)
		{
			try
			{
				FieldInfo fi = f.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				object val = ((fi != null) ? fi.GetValue(f) : null);
				if (val is Button btn)
				{
					btn.PerformClick();
				}
				else if (val is ToolStripItem tsi)
				{
					tsi.PerformClick();
				}
				else if (val is Control ctrl)
				{
					MethodInfo onClick = ctrl.GetType().GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
					if (onClick != null)
					{
						onClick.Invoke(ctrl, new object[1] { EventArgs.Empty });
					}
				}
			}
			catch
			{
			}
		}

		private static void ExportProfile(MainForm f)
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "Profile (*.cdtp)|*.cdtp";
				sfd.Title = "Export Encrypted Profile";
				if (sfd.ShowDialog(f) != DialogResult.OK)
				{
					return;
				}
				string pass = PromptPassphrase(f, "Enter passphrase to encrypt profile");
				if (pass != null)
				{
					try
					{
						f.ProfileService.Export(sfd.FileName, pass);
						MessageBox.Show(f, "Profile exported.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						return;
					}
					catch (Exception ex)
					{
						MessageBox.Show(f, "Export failed: " + ex.Message, "Export", MessageBoxButtons.OK, MessageBoxIcon.Hand);
						return;
					}
				}
			}
		}

		private static void ImportProfile(MainForm f)
		{
			using (OpenFileDialog ofd = new OpenFileDialog())
			{
				ofd.Filter = "Profile (*.cdtp)|*.cdtp";
				ofd.Title = "Import Encrypted Profile";
				if (ofd.ShowDialog(f) != DialogResult.OK)
				{
					return;
				}
				string pass = PromptPassphrase(f, "Enter passphrase to decrypt profile");
				if (pass != null)
				{
					try
					{
						f.ProfileService.Import(ofd.FileName, pass);
						MessageBox.Show(f, "Profile imported.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						return;
					}
					catch (Exception ex)
					{
						MessageBox.Show(f, "Import failed: " + ex.Message, "Import", MessageBoxButtons.OK, MessageBoxIcon.Hand);
						return;
					}
				}
			}
		}

		private static string PromptPassphrase(Form f, string title)
		{
			Form dlg = new Form();
			dlg.Text = title;
			dlg.StartPosition = FormStartPosition.CenterParent;
			dlg.Width = 420;
			dlg.Height = 160;
			TableLayoutPanel tl = new TableLayoutPanel();
			tl.Dock = DockStyle.Fill;
			tl.ColumnCount = 1;
			tl.RowCount = 3;
			dlg.Controls.Add(tl);
			tl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			TextBox tb = new TextBox();
			tb.PasswordChar = '●';
			tb.Width = 360;
			tb.Margin = new Padding(12);
			tl.Controls.Add(tb, 0, 0);
			FlowLayoutPanel bar = new FlowLayoutPanel();
			bar.FlowDirection = FlowDirection.RightToLeft;
			bar.Margin = new Padding(12);
			Button ok = new Button();
			ok.Text = "OK";
			Button cancel = new Button();
			cancel.Text = "Cancel";
			ok.Click += delegate
			{
				dlg.DialogResult = DialogResult.OK;
			};
			cancel.Click += delegate
			{
				dlg.DialogResult = DialogResult.Cancel;
			};
			bar.Controls.Add(ok);
			bar.Controls.Add(cancel);
			tl.Controls.Add(bar, 0, 1);
			return (dlg.ShowDialog(f) == DialogResult.OK) ? tb.Text : null;
		}

		private static void OpenLogFolder()
		{
			try
			{
				string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite", "logs");
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				Process.Start("explorer.exe", dir);
			}
			catch
			{
			}
		}

		private static void CopyRecentLogToClipboard(Form f)
		{
			try
			{
				string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite", "logs");
				if (!Directory.Exists(dir))
				{
					MessageBox.Show(f, "No logs yet.", "Logs", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					return;
				}
				string[] files = Directory.GetFiles(dir, "*.log");
				if (files.Length == 0)
				{
					MessageBox.Show(f, "No logs yet.", "Logs", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					return;
				}
				string last = files[files.Length - 1];
				Clipboard.SetText(File.ReadAllText(last, Encoding.UTF8));
				MessageBox.Show(f, "Copied.", "Logs", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
			catch (Exception ex)
			{
				MessageBox.Show(f, "Copy failed: " + ex.Message, "Logs", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}

		private static void OpenReadme()
		{
			try
			{
				string readmePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "README.md");
				if (File.Exists(readmePath))
				{
					Process.Start("notepad.exe", readmePath);
				}
			}
			catch
			{
			}
		}
	}
}
