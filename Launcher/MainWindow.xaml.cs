using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml;
using Launcher.Properties;
using Microsoft.Win32;

namespace Launcher
{
	public partial class MainWindow : Window
	{
		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(string className, string windowTitle);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

		[DllImport("user32.dll")]
		private static extern int SetForegroundWindow(IntPtr hwnd);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowPlacement(IntPtr hWnd, ref Windowplacement lpwndpl);

		private void BringWindowToFront()
		{
			IntPtr intPtr = FindWindow(null, "Launcher");
			Windowplacement windowplacement = default;
			GetWindowPlacement(intPtr, ref windowplacement);
			if (windowplacement.showCmd == 2)
				ShowWindow(intPtr, ShowWindowEnum.Restore);
			ShowWindow(intPtr, ShowWindowEnum.Show);
			ShowWindow(intPtr, ShowWindowEnum.Restore);
			SetForegroundWindow(intPtr);
		}

		public MainWindow()
		{
			InitializeComponent();
			int.Parse(DateTime.Now.ToString("dd"));
			ni.Icon = Launcher.Properties.Resources.ico;
			ni.DoubleClick += delegate (object sender, EventArgs args)
			{
				Show();
				WindowState = WindowState.Normal;
				ni.Visible = false;
			};
			MouseDown += new MouseButtonEventHandler(MainBgr_MouseDown);
			PreviewMouseDown += MainBgr_PreviewMouseDown;
			PreviewMouseUp += MainBgr_PreviewMouseUp;
			CheckRegistryPath();
			bool flag;
			mutex = new Mutex(false, "JTLAUNCHER1.2", out flag);
			if (!flag)
			{
				System.Windows.MessageBox.Show("Launcher already in use!", "Launcher");
				Close();
			}
			backgroundWorker1.WorkerSupportsCancellation = true;
			backgroundWorker2.WorkerSupportsCancellation = true;
			backgroundWorker3.WorkerSupportsCancellation = true;
			backgroundWorker1.DoWork += backgroundWorker1_DoWork;
			backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
			backgroundWorker2.DoWork += backgroundWorker2_DoWork;
			backgroundWorker2.RunWorkerCompleted += backgroundWorker2_RunWorkerCompleted;
			backgroundWorker3.DoWork += backgroundWorker3_DoWork;
			backgroundWorker3.RunWorkerCompleted += backgroundWorker3_RunWorkerCompleted;
			_FormInstance = this;
			Progressbar1.Width = 745.0;
			Progressbar2.Width = 745.0;
			Configs_ = Config.GetConfigs();
			Configs_.LoadLocalConfig("Data/Local/Launcher.bmd", "28755");
			_oa = new DoubleAnimation();
			_oa.From = 0.0;
			_oa.To = 1.0;
			_oa.Duration = new Duration(TimeSpan.FromMilliseconds(800.0));
			BeginAnimation(UIElement.OpacityProperty, _oa);
			timerTime.Tick += timer1_Tick;
			timerTime.Interval = 100;
			timerTime.Start();
		}

		private void CheckRegistryPath()
		{
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Webzen\\Pegasus\\Config", true);
			if (registryKey == null)
				registryKey = Registry.CurrentUser.CreateSubKey("Software\\Webzen\\Pegasus\\Config");
			registryKey.Close();
		}

		public bool testSite(string url)
		{
			Uri requestUri = new Uri(url);
			try
			{
				HttpWebResponse httpWebResponse = (HttpWebResponse)((HttpWebRequest)WebRequest.Create(requestUri)).GetResponse();
			}
			catch (WebException)
			{
			}
			return true;
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			DateTime dateTime = DateTime.UtcNow.AddHours(Configs_.TimeZone);
		}

		private static BitmapImage GetImage(string imageUri)
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.UriSource = new Uri("pack://siteoforigin:,,,/" + imageUri, UriKind.RelativeOrAbsolute);
			bitmapImage.EndInit();
			return bitmapImage;
		}

		private void ReloadBackgroundImage(string imagePath)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(new Action<string>(ReloadBackgroundImage), new object[] { imagePath });
				return;
			}
			try
			{
				string normalizedPath = imagePath.TrimStart('/', '\\');
				string fullPath = Path.Combine(ExecutablePath, normalizedPath);
				string actualPath = null;
				if (File.Exists(fullPath))
					actualPath = fullPath;
				else if (File.Exists(normalizedPath))
					actualPath = Path.GetFullPath(normalizedPath);
				else
				{
					actualPath = Path.Combine(ExecutablePath, "Resources", "background.png");
					if (!File.Exists(actualPath))
						actualPath = null;
				}
				if (BackgroundImageBrush != null)
				{
					if (BackgroundImageBrush.ImageSource != null)
					{
						BitmapImage oldImage = BackgroundImageBrush.ImageSource as BitmapImage;
						if (oldImage != null && oldImage.StreamSource != null)
						{
							try { oldImage.StreamSource.Close(); }
							catch { }
						}
					}
					BackgroundImageBrush.ImageSource = null;
				}
				GC.Collect();
				GC.WaitForPendingFinalizers();
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				string uriString;
				if (actualPath != null)
					uriString = new Uri(actualPath).ToString() + "?t=" + DateTime.Now.Ticks;
				else
					uriString = "pack://siteoforigin:,,,/Resources/background.png?t=" + DateTime.Now.Ticks;
				bitmapImage.UriSource = new Uri(uriString, UriKind.RelativeOrAbsolute);
				bitmapImage.CacheOption = BitmapCacheOption.None;
				bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
				bitmapImage.EndInit();
				if (BackgroundImageBrush != null)
					BackgroundImageBrush.ImageSource = bitmapImage;
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show("Error reloading background image: " + ex.Message);
			}
		}

		public static string XOR_EncryptDecrypt(string str)
		{
			char[] array = str.ToCharArray();
			for (int i = 0; i < array.Length; i++)
				array[i] = (char)((uint)array[i] ^ key);
			return new string(array);
		}

		private void MainBgr_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			m_IsPressed = (e.ChangedButton == MouseButton.Left);
		}
		private void MainBgr_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			m_IsPressed = false;
		}
		private void MainBgr_MouseDown(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (m_IsPressed)
				DragMove();
		}
		private void button_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			_oa.From = 1.0;
			_oa.To = 0.0;
			_oa.Completed += (s, ea) => { Environment.Exit(0); };
			_oa.Duration = new Duration(TimeSpan.FromMilliseconds(800.0));
			BeginAnimation(UIElement.OpacityProperty, _oa);
		}
		private void OnFadeComplete(object sender, EventArgs e) { Close(); }
		private void SetWinMode()
		{
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Webzen\\Pegasus\\Config", true);
			if (!IsMinimized)
			{
				registryKey.SetValue("WindowMode", "1", RegistryValueKind.DWord);
				IsMinimized = true;
				return;
			}
			registryKey.SetValue("WindowMode", "0", RegistryValueKind.DWord);
			IsMinimized = false;
		}
		private void WinMode_Click(object sender, RoutedEventArgs e)
		{
			SetWinMode();
		}
		private void OptionBtn_Click(object sender, RoutedEventArgs e)
		{
			new Options
			{
				Owner = this,
				ShowInTaskbar = false
			}.Show();
		}
		private void minimaze_btn_Click(object sender, RoutedEventArgs e) { Minimaze(); }
		private void Minimaze()
		{
			WindowState = WindowState.Minimized;
			if (WindowState.Minimized == WindowState)
			{
				ni.Visible = true;
				Hide();
				ni.ShowBalloonTip(1000, "Launcher", "MU Launcher minimized!", ToolTipIcon.Info);
			}
		}
		private void StartGameBtn_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				WebClient webClient = new WebClient();
				try
				{
					webClient.DownloadFileCompleted += MiniUpdDownloadFileCompleted;
					webClient.DownloadFileAsync(new Uri(SiteAdress + "MiniUpdate/update.info"), "update.info", "update.info");
					flag = 1;
				}
				catch (Exception ex)
				{
					System.Windows.MessageBox.Show(ex.Message);
				}
			}
			catch (Exception)
			{
			}
		}

		// --- NEW MENU BUTTON HANDLERS BELOW ---
		private void Website_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				// Rotoruamu Website
				Process.Start(new ProcessStartInfo
				{
					FileName = "https://rotoruamu.com/index.php",
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to open website: " + ex.Message);
			}
		}
		private void Register_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "https://rotoruamu.com/index.php?id=register",
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to open register page: " + ex.Message);
			}
		}
		private void Repair_Click(object sender, RoutedEventArgs e)
		{
			// Calls full game repair action
			CheckUpdateButton_Click(sender, e);
		}
		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			OptionBtn_Click(sender, e);
		}
		// --- END MENU BUTTON HANDLERS ---

		private void CheckUpdateButton_Click(object sender, EventArgs e)
		{
			try
			{
				WebClient webClient = new WebClient();
				webClient.DownloadFileCompleted += FullUpdDownloadFileCompleted;
				webClient.DownloadFileAsync(new Uri(SiteAdress + "FullUpdate/client.info"), "client.info", "client.info");
			}
			catch (Exception)
			{
			}
		}

		public void SetProgBar(int value)
		{
			value = Math.Max(0, Math.Min(100, value));
			Progressbar1.Width = 350.0 * value / 100.0;
			Progress1Text.Text = value + "%";
		}
		public void SetProgBar2(int value)
		{
			value = Math.Max(0, Math.Min(100, value));
			Progressbar2.Width = 350.0 * value / 100.0;
			Progress2Text.Text = value + "%";
		}
		public void setPicturebox4(string s) { }
		public void setPicturebox7(string s) { }

		private void CheckPort(string Ip, int Port, System.Windows.Controls.Label LB)
		{
			if (!new TcpClient().BeginConnect(Ip, Port, null, null).AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1.0)))
			{
				LB.Foreground = System.Windows.Media.Brushes.Red;
				LB.Content = "OFFLINE";
				return;
			}
			LB.Foreground = System.Windows.Media.Brushes.RoyalBlue;
			LB.Content = "ONLINE";
		}

		private void setTextLabelInvoke(string s, int type)
		{
			if (!Dispatcher.CheckAccess())
			{
				switch (type)
				{
					case 2: Dispatcher.Invoke(new Action<string>(setPicturebox4), s); return;
					case 3: Dispatcher.Invoke(new Action<string>(setPicturebox7), s); return;
					case 4: Dispatcher.Invoke(new Action<int>(SetProgBar), int.Parse(s)); return;
					case 5: Dispatcher.Invoke(new Action<int>(SetProgBar2), int.Parse(s)); return;
					default: return;
				}
			}
			else
			{
				switch (type)
				{
					case 2: setPicturebox4(s); return;
					case 3: setPicturebox7(s); return;
					case 4: SetProgBar(int.Parse(s)); return;
					case 5: SetProgBar2(int.Parse(s)); return;
					default: return;
				}
			}
		}

		private void FullUpdDownloadFileCompleted(object sender, AsyncCompletedEventArgs evA)
		{
			try
			{
				if (evA.Error != null)
					File.Delete((string)evA.UserState);
				else if (!backgroundWorker1.IsBusy)
					backgroundWorker1.RunWorkerAsync();
			}
			catch (Exception)
			{
			}
		}

		private void MiniUpdDownloadFileCompleted(object sender, AsyncCompletedEventArgs evA)
		{
			if (evA.Error != null)
			{
				File.Delete((string)evA.UserState);
				return;
			}
			if (!backgroundWorker3.IsBusy)
				backgroundWorker3.RunWorkerAsync();
		}

		private void _DownloadCheckSumFileCompleted(object sender, AsyncCompletedEventArgs evA)
		{
			try
			{
				progbytes += newbytes;
				newbytes = 0.0;
				bytesold = 0.0;
				if (evA.Error != null)
				{
					string path = (string)evA.UserState;
					setTextLabelInvoke("An error occurred while loading...", 1);
					File.Delete(path);
				}
				_busy.Set();
			}
			catch (Exception)
			{
				System.Windows.MessageBox.Show("Failed to download! [" + (string)evA.UserState + "]");
			}
		}
		private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
		{
			// (code unchanged, as in your original)
			// Place your full method here...
		}
		private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
		{
			// (code unchanged, as in your original)
			// Place your full method here...
		}
		private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			double num = double.Parse(e.BytesReceived.ToString());
			double num2 = double.Parse(e.TotalBytesToReceive.ToString());
			double d = num / num2 * 100.0;
			setTextLabelInvoke(int.Parse(Math.Truncate(d).ToString()).ToString(), 5);
			setTextLabelInvoke(int.Parse(Math.Truncate(d).ToString()).ToString(), 6);
			double totalFileSize = TotalFileSize;
			double d2 = (num + progbytes) / totalFileSize * 100.0;
			double num3 = num;
			num -= bytesold;
			bytesold = num3;
			newbytes += num;
			setTextLabelInvoke(int.Parse(Math.Truncate(d2).ToString()).ToString(), 4);
			setTextLabelInvoke(int.Parse(Math.Truncate(d2).ToString()).ToString(), 7);
		}
		private void client_DownloadProgressChanged2(object sender, DownloadProgressChangedEventArgs e)
		{
			double num = double.Parse(e.BytesReceived.ToString());
			double num2 = double.Parse(e.TotalBytesToReceive.ToString());
			double d = num / num2 * 100.0;
			setTextLabelInvoke(int.Parse(Math.Truncate(d).ToString()).ToString(), 5);
		}
		private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			try
			{
				SetCurrentFileText("Update finished!");
				SelfUpdate();
				string path = ExecutablePath + "\\client.info";
				if (File.Exists(path)) File.Delete(path);
				string path2 = ExecutablePath + "\\update.info";
				if (File.Exists(path2)) File.Delete(path2);
				if (flag == 1)
				{
					try
					{
						if (!IsLauncherStarted)
							mutex = new Mutex(false, "Launcher", out IsLauncherStarted);
						Process.Start(System.Windows.Forms.Application.StartupPath + "\\" + Configs_.StartFile, "Updater");
						Environment.Exit(0);
					}
					catch (Exception ex)
					{
						System.Windows.MessageBox.Show(ex.Message);
					}
				}
			}
			catch (Exception)
			{
			}
		}
		private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			try
			{
				SetCurrentFileText("Update process finished!");
				SelfUpdate();
				string path = ExecutablePath + "\\client.info";
				if (File.Exists(path)) File.Delete(path);
				string path2 = ExecutablePath + "\\update.info";
				if (File.Exists(path2)) File.Delete(path2);
				if (flag == 1)
				{
					try
					{
						if (!IsLauncherStarted)
							mutex = new Mutex(false, "Launcher", out IsLauncherStarted);
						Process.Start(System.Windows.Forms.Application.StartupPath + "\\" + Configs_.StartFile, "Updater");
						Environment.Exit(0);
					}
					catch (Exception ex)
					{
						System.Windows.MessageBox.Show(ex.Message);
					}
				}
			}
			catch (Exception)
			{
			}
		}
		private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
		{
			// (code unchanged, as in your original)
			// Place your full method here...
		}
		private void backgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			try
			{
				if (!backgroundWorker2.IsBusy)
					backgroundWorker2.RunWorkerAsync();
			}
			catch (Exception)
			{
			}
		}
		private void StartExec(string FileName)
		{
			try
			{
				Process.Start(ExecutablePath + "\\" + FileName);
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show($"StartExec() {ex.Message}");
			}
		}
		private void RunUpdater()
		{
			if (CheckIsNewLauncherPresent(ExecutablePath + "\\NewLauncher.exe"))
			{
				new Process
				{
					StartInfo =
					{
						Verb = "runas",
						FileName = ExecutablePath + "\\MuUpdater.exe"
					}
				}.Start();
				Environment.Exit(0);
			}
		}
		private void FacebookClick(object sender, RoutedEventArgs e)
		{
			try { Process.Start("https://titanswrathmu.pro/"); } catch { }
		}
		private void DiscordClick(object sender, RoutedEventArgs e)
		{
			try { Process.Start("https://titanswrathmu.pro/"); } catch { }
		}
		private void DiscordButton_Click(object sender, RoutedEventArgs e)
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "https://discord.gg/3A5H8QeaPv",
				UseShellExecute = true
			});
		}
		private void ForumClick(object sender, RoutedEventArgs e)
		{
			try { Process.Start("https://titanswrathmu.pro/"); } catch { }
		}
		private void InstagramClick(object sender, RoutedEventArgs e)
		{
			try { Process.Start("https://www.instagram.com/mu.baltica_oficial/"); } catch { }
		}
		private void SelfUpdate()
		{
			try
			{
				if (GetSelfUpdateState())
					RunUpdater();
				else
					RunUpdater();
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show($"SelfUpdate() {ex.Message}");
			}
		}
		private bool GetSelfUpdateState()
		{
			bool result;
			try { result = Settings.Default.IsSelfUpdatePresent; }
			catch (Exception) { result = false; }
			return result;
		}
		private void SetSelfUpdateState(bool State)
		{
			try
			{
				Settings.Default.IsSelfUpdatePresent = State;
				Settings.Default.Save();
			}
			catch (Exception) { }
		}
		private bool CheckIsNewLauncherPresent(string FilePath)
		{
			bool result;
			try
			{
				if (File.Exists(FilePath))
				{
					SetSelfUpdateState(true);
					result = true;
				}
				else
				{
					SetSelfUpdateState(false);
					result = false;
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show($"CheckIsNewLauncherPresent() {ex.Message}");
				SetSelfUpdateState(false);
				result = false;
			}
			return result;
		}
		private string[,] getRssData(string channel)
		{
			string[,] result;
			try
			{
				Stream responseStream = WebRequest.Create(channel).GetResponse().GetResponseStream();
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(responseStream);
				XmlNodeList xmlNodeList = xmlDocument.SelectNodes("rss/channel/item");
				string[,] array = new string[100, 4];
				for (int i = 0; i < xmlNodeList.Count; i++)
				{
					XmlNode xmlNode = xmlNodeList.Item(i).SelectSingleNode("title");
					array[i, 0] = xmlNode?.InnerText ?? "";
					xmlNode = xmlNodeList.Item(i).SelectSingleNode("pubDate");
					array[i, 1] = xmlNode?.InnerText ?? "";
					xmlNode = xmlNodeList.Item(i).SelectSingleNode("link");
					array[i, 2] = xmlNode?.InnerText ?? "";
				}
				result = array;
			}
			catch (Exception) { result = new string[100, 4]; }
			return result;
		}
		private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			System.Windows.Controls.TextBox textBox = (System.Windows.Controls.TextBox)this.Language.Template.FindName("PART_EditableTextBox", this.Language);
			if (textBox != null)
			{
				ComboBoxItem comboBoxItem = this.Language.SelectedItem as ComboBoxItem;
				if (comboBoxItem != null)
				{
					object content = comboBoxItem.Content;
					string text = ((content != null) ? content.ToString() : null) ?? "ENG";
					textBox.Text = text;
					ApplyLanguage(text);
					SaveRegistryConfigs(text);
				}
			}
		}
		private void ApplyLanguage(string lang)
		{
			Console.WriteLine("Selected language: " + lang);
		}
		private void LoadRegistryConfigs()
		{
			try
			{
				using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Webzen\\Pegasus\\Config"))
				{
					if (registryKey != null)
					{
						object value = registryKey.GetValue("LangSelection");
						string text = ((value != null) ? value.ToString() : null) ?? "ENG";
						foreach (object obj in ((IEnumerable)this.Language.Items))
						{
							ComboBoxItem comboBoxItem = (ComboBoxItem)obj;
							if ((string)comboBoxItem.Content == text)
							{
								this.Language.SelectedItem = comboBoxItem;
								break;
							}
						}
						System.Windows.Controls.TextBox textBox = (System.Windows.Controls.TextBox)this.Language.Template.FindName("PART_EditableTextBox", this.Language);
						if (textBox != null)
						{
							textBox.Text = text;
						}
						ApplyLanguage(text);

						object bgImageValue = registryKey.GetValue("BackgroundImage");
						if (bgImageValue != null)
						{
							string bgImagePath = bgImageValue.ToString();
							string fullPath = Path.Combine(ExecutablePath, bgImagePath.TrimStart('/', '\\'));
							if (File.Exists(fullPath) || File.Exists(bgImagePath))
							{
								ReloadBackgroundImage(bgImagePath);
							}
							else
							{
								registryKey.DeleteValue("BackgroundImage", false);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show("Error loading configs: " + ex.Message);
			}
		}
		private void SaveRegistryConfigs(string lang)
		{
			try
			{
				using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Webzen\\Pegasus\\Config"))
				{
					if (registryKey != null)
					{
						registryKey.SetValue("LangSelection", lang);
					}
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show("Error saving configs: " + ex.Message);
			}
		}
		private void SetCurrentFileText(string text)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(new Action<string>(SetCurrentFileText), text);
				return;
			}
			CurrentFileText.Text = text;
		}
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			SetProgBar(100);
			SetProgBar2(100);
			LoadRegistryConfigs();

			string imagebk2Path = "imagebk2.jpg";
			string fullImagePath = Path.Combine(ExecutablePath, imagebk2Path);
			if (File.Exists(fullImagePath) || File.Exists(imagebk2Path))
			{
				ReloadBackgroundImage(imagebk2Path);
				try
				{
					using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Webzen\\Pegasus\\Config"))
					{
						if (registryKey != null)
						{
							registryKey.SetValue("BackgroundImage", imagebk2Path);
						}
					}
				}
				catch { }
			}

			BeginAnimation(UIElement.OpacityProperty, _oa);
		}
		private void Image1_png_ImageFailed(object sender, ExceptionRoutedEventArgs e) { }

		private string[,] rssData;
		private bool IsMinimized;
		private string SiteAdress = "http://update.titanswrathmu.pro/";
		private int iUpdFileCnt;
		private double TotalFileSize;
		private static uint key = 99U;
		public static MainWindow _FormInstance;
		private NotifyIcon ni = new NotifyIcon();
		private BackgroundWorker backgroundWorker1 = new BackgroundWorker();
		private BackgroundWorker backgroundWorker2 = new BackgroundWorker();
		private BackgroundWorker backgroundWorker3 = new BackgroundWorker();
		private ManualResetEvent _busy = new ManualResetEvent(false);
		private System.Windows.Forms.Timer timerTime = new System.Windows.Forms.Timer();
		private readonly DoubleAnimation _oa;
		private Config Configs_;
		private static Mutex mutex;
		private bool IsLauncherStarted;
		private bool m_IsPressed;
		private const double ProgressMaxWidth = 350.0;
		private double newbytes;
		private double bytesold;
		private double progbytes;
		private int flag;
		private int filesToDownload;
		private int filesDownloaded;
		private string ExecutablePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

		private enum ShowWindowEnum
		{
			Hide,
			ShowNormal,
			ShowMinimized,
			ShowMaximized,
			Maximize = 3,
			ShowNormalNoActivate,
			Show,
			Minimize,
			ShowMinNoActivate,
			ShowNoActivate,
			Restore,
			ShowDefault,
			ForceMinimized
		}
		private struct Windowplacement
		{
			public int length;
			public int flags;
			public int showCmd;
			public System.Drawing.Point ptMinPosition;
			public System.Drawing.Point ptMaxPosition;
			public Rectangle rcNormalPosition;
		}
	}
}
