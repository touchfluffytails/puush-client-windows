using System;
using System.Collections.Generic;
using System.Text;
using osu_common.Libraries.NetLib;
using System.Windows.Forms;
using System.Threading;
using System.Windows.Forms.VisualStyles;
using System.IO;

namespace puush
{
    internal static class HistoryManager
    {
        internal static List<HistoryItem> HistoryItems = new List<HistoryItem>();

        internal static bool UpdateScheduled;

		internal static void Update()
		{
			if (!puush.IsLoggedIn)
			{
				return;
			}

			FormNetRequest request = new FormNetRequest(puush.getApiUrl("hist"));
			request.request.Items.AddFormField("k", puush.config.GetValue<string>("key", ""));
			request.onFinish += new FormNetRequest.RequestCompleteHandler(historyRetrieval_onFinish);

			NetManager.AddRequest(request);
		}

		internal static void Update(string filepath)
		{
			bool uploadInternet = puush.config.GetValue<bool>("uploadtointernet", true);

			if (uploadInternet)
			{
				return;
			}

			int updateSize = puush.config.GetValue<int>("historysize", 5);
			int startIndex = 0;
			bool newFile = false;

			if (!string.IsNullOrEmpty(filepath))
			{
				updateSize++;
				startIndex = 1;
				newFile = true;
			}

			string[] currentHistory = new string[updateSize];
			if (!string.IsNullOrEmpty(filepath))
			{
				currentHistory[0] = filepath;
			}

			for (int i = startIndex; i < updateSize; i++)
			{
				currentHistory[i] = puush.config.GetValue<string>($"history{i-1}", string.Empty);
			}

			if (newFile)
			{
				int historySize = puush.config.GetValue<int>("historysize", 5);
				for (int i = 0; i < historySize; i++)
				{
					puush.config.SetValue<string>($"history{i}", currentHistory[i]);
				}
			}

			UpdateOfflineHistory();
		}

		internal static void UpdateOfflineHistory()
		{
			MainForm.Instance.Invoke(delegate
			{
				ContextMenuStrip menu = MainForm.Instance.contextMenuStrip1;

				foreach (HistoryItem i in HistoryItems)
				{
					menu.Items.Remove(i);
					i.Dispose();
				}

				HistoryItems.Clear();

				string dateFormat = string.Empty;
				SavedFilenameFormat filenameFormat = (SavedFilenameFormat)puush.config.GetValue<int>("savedfilenameformat", 0);

				switch (filenameFormat)
				{
					case SavedFilenameFormat.TwentyfourHours:
						dateFormat = "{0:yyyy-MM-dd} {0:HH.mm.ss}";
						break;
					case SavedFilenameFormat.TwelveHours:
					default:
						// If it isn't valid just use default
						dateFormat = "{0:yyyy-MM-dd} {0:hh.mm.ss}";
						break;
				}

				try
				{
					int historySize = puush.config.GetValue<int>("historysize", 5);
					string[] currentHistory = new string[historySize];

					for (int i = 0; i < historySize; i++)
					{
						currentHistory[i] = puush.config.GetValue<string>($"history{i}", string.Empty);
					}


					for(int i = 0; i < currentHistory.Length; i++)
					{
						int id = i;
						string url = currentHistory[i];
						string filename = Path.GetFileName(currentHistory[i]);
						int viewCount = 0;
						string date = string.Empty;

						FileInfo fileInfo = new FileInfo(filename);

						if (fileInfo.Exists)
						{
							DateTime dateTime = fileInfo.CreationTimeUtc.ToLocalTime();

							date = dateTime.ToString(dateFormat);
						}

						HistoryItems.Insert(0, new HistoryItem(id, date, url, filename, viewCount));
					}
				}
				catch { }

				foreach (HistoryItem i in HistoryItems) menu.Items.Insert(4, i);

			});

		}

		internal static void historyRetrieval_onFinish(string _result, Exception e)
        {
            // if (!UpdateScheduled)
            // {
            //     UpdateScheduled = true;

            //     MainForm.threadMeSome(delegate {
            //         Thread.Sleep(60000 * 5); //wait 5 minutes
            //         UpdateScheduled = false;
            //         Update();
            //     });
            // }

            if (e != null) return;

            MainForm.Instance.Invoke(delegate
            {
                ContextMenuStrip menu = MainForm.Instance.contextMenuStrip1;

                foreach (HistoryItem i in HistoryItems)
                {
                    menu.Items.Remove(i);
                    i.Dispose();
                }

                HistoryItems.Clear();

                int response;

                string[] lines = _result.Split('\n');

                if (!Int32.TryParse(lines[0], out response))
                    response = -2;

                switch (response)
                {
                    case -1:
                        //puush.HandleInvalidAuthentication();
                        //todo: reimplement this after server-side handles correctly.
                        return;
                    case -2:
                        //unknown/other
                        return;
                }


                int displayCount = 5;

                try
                {

                    bool firstLine = true;
                    foreach (string line in lines)
                    {
                        if (firstLine)
                        {
                            firstLine = false;
                            continue;
                        }

                        if (displayCount-- == 0) break;

                        if (line.Length == 0) break;

                        string[] parts = line.Split(',');

                        int id = Int32.Parse(parts[0]);
                        string date = parts[1];
                        string url = parts[2];
                        string filename = parts[3];
                        int viewCount = Int32.Parse(parts[4]);

                        HistoryItems.Insert(0,new HistoryItem(id, date, url, filename,viewCount));
                    }
                }
                catch { }

                foreach (HistoryItem i in HistoryItems) menu.Items.Insert(4, i);

            });

        }
    }
}
