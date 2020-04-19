﻿using HttpProgress;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    class UpdaterWindowViewModel
    {
        private string changelogText;
        public string ChangelogText {
            get => changelogText;
            private set
            {
                if (changelogText == value) return;
                changelogText = value;
                ChangelogTextChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ChangelogTextChanged;

        private string newversion;
        public string Newversion { get => newversion; }


        private FlowDocument changelogDocument;
        public FlowDocument ChangelogDocument
        {
            get => changelogDocument;
            private set
            {
                if (changelogDocument == value) return;
                changelogDocument = value;
                ChangelogDocumentChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ChangelogDocumentChanged;


        public UpdaterWindowViewModel(string newversion)
        {
            changelogText = "Retrieving changelog info. Please wait...";
            this.newversion = newversion;
            //RetrieveChangelogInfo();
        }

        public async void RetrieveChangelogInfo()
        {
            // Sorry other devs, gonna have to find your own server
            Uri url = new Uri("https://raw.githubusercontent.com/Ryochan7/DS4Windows/changelog_draft/DS4Windows/Changelog.min.json");
            string filename = Path.Combine(Path.GetTempPath(), "Changelog.min.json");
            bool readFile = false;
            using (var downloadStream = new FileStream(filename, FileMode.Create))
            {
                Task<System.Net.Http.HttpResponseMessage> temp = App.requestClient.GetAsync(url.ToString(), downloadStream);
                await temp.ConfigureAwait(true);

                if (temp.Result.IsSuccessStatusCode) readFile = true;
                else ChangelogText = "Failed to retrieve information";
            }

            //await Task.Run(() => { });
            //string filename = @"C:\Users\ryoch\source\repos\DS4Windows\DS4Windows\test.json";
            //bool readFile = true;
            bool fileExists = File.Exists(filename);
            if (fileExists && readFile)
            {
                string temp = File.ReadAllText(filename).Trim();
                try
                {
                    ChangelogInfo tempInfo = JsonConvert.DeserializeObject<ChangelogInfo>(temp);
                    //BuildChangelogString(tempInfo);
                    BuildChangelogDocument(tempInfo);
                }
                catch (JsonSerializationException) { }
            }

            if (fileExists)
            {
                File.Delete(filename);
            }
        }

        private void BuildChangelogString(ChangelogInfo tempInfo)
        {
            string temp = string.Empty;
            foreach (ChangeVersionInfo versionInfo in tempInfo.Changelog.Versions)
            {
                //temp += string.Join("\n", versionInfo.ApplicableInfo(DS4Windows.Global.UseLang).LogText);
                VersionLogLocale tmpLog = versionInfo.ApplicableInfo(DS4Windows.Global.UseLang);
                if (tmpLog != null)
                {
                    temp += tmpLog.Header + "\n\n";
                    tmpLog.BuildDisplayText();
                    temp += tmpLog.DisplayLogText;
                    temp += "\n\n";
                }
            }

            ChangelogText = temp;
        }

        private void BuildChangelogDocument(ChangelogInfo tempInfo)
        {
            FlowDocument flow = new FlowDocument();
            foreach (ChangeVersionInfo versionInfo in tempInfo.Changelog.Versions)
            {
                VersionLogLocale tmpLog = versionInfo.ApplicableInfo(DS4Windows.Global.UseLang);
                if (tmpLog != null)
                {
                    Paragraph tmpPar = new Paragraph() { Tag = "Header" };
                    string tmp = tmpLog.Header;
                    tmpPar.Inlines.Add(new Run(tmp));
                    flow.Blocks.Add(tmpPar);

                    //tmpLog.BuildDisplayText();
                    //tmpPar.Inlines.Add(new Run(tmpLog.DisplayLogText));

                    List versList = new List();
                    foreach (string commit in tmpLog.LogText)
                    {
                        tmp = commit.Trim('*');
                        tmpPar = new Paragraph(new Run(tmp));
                        versList.ListItems.Add(new ListItem(tmpPar));

                        tmp = commit.Trim('*');
                        tmpPar = new Paragraph(new Run(tmp));
                        versList.ListItems.Add(new ListItem(tmpPar));

                        flow.Blocks.Add(versList);
                    }

                    tmpPar = new Paragraph();
                    flow.Blocks.Add(tmpPar);
                }
            }

            ChangelogDocument = flow;
        }
    }

    public class ChangelogInfo
    {
        private string latestVersion;
        private DateTime updatedAt;
        private ChangelogVersions changelog;

        [JsonProperty("latest_version")]
        public string LatestVersion { get => latestVersion; set => latestVersion = value; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get => updatedAt; set => updatedAt = value; }

        [JsonProperty("changelog")]
        public ChangelogVersions Changelog { get => changelog; set => changelog = value; }
    }

    public class ChangelogVersions
    {
        private List<ChangeVersionInfo> versions;

        [JsonProperty("versions")]
        public List<ChangeVersionInfo> Versions { get => versions; set => versions = value; }
    }

    public class ChangeVersionInfo
    {
        private string version;
        private string baseHeader;
        private DateTime releaseDate;
        private List<VersionLogLocale> versionLocales;

        [JsonProperty("version_str")]
        public string Version { get => version; set => version = value; }

        [JsonProperty("base_header")]
        public string BaseHeader { get => baseHeader; set => baseHeader = value; }

        [JsonProperty("release_date")]
        public DateTime ReleaseDate { get => releaseDate; set => releaseDate = value; }

        [JsonProperty("locales")]
        public List<VersionLogLocale> VersionLocales { get => versionLocales; set => versionLocales = value; }

        public VersionLogLocale ApplicableInfo(string culture)
        {
            Dictionary<string, VersionLogLocale> tempDict =
                new Dictionary<string, VersionLogLocale>();

            foreach (VersionLogLocale logLoc in versionLocales)
            {
                tempDict.Add(logLoc.Code, logLoc);
            }

            VersionLogLocale result = null;
            CultureInfo hairyLegs = null;
            try
            {
                if (!string.IsNullOrEmpty(culture))
                {
                    hairyLegs = CultureInfo.GetCultureInfo(culture);
                }
            }
            catch (CultureNotFoundException) { }

            if (hairyLegs != null)
            {
                if (tempDict.ContainsKey(hairyLegs.Name))
                {
                    result = tempDict[hairyLegs.Name];
                }
                else if (tempDict.ContainsKey(hairyLegs.TwoLetterISOLanguageName))
                {
                    result =
                        tempDict[hairyLegs.TwoLetterISOLanguageName];
                }
            }

            if (result == null && versionLocales.Count > 0)
            {
                // Default to first entry if specific culture info not found
                result = versionLocales[0];
            }

            return result;
        }
    }

    public class VersionLogLocale
    {
        private string code;
        private string header;
        private List<string> logText;
        private string editor;
        private List<string> editorsNote;
        private DateTime editedAt;

        private string displayLogText;
        public string DisplayLogText { get => displayLogText; }

        public string Code { get => code; set => code = value; }
        public string Header { get => header; set => header = value; }

        [JsonProperty("log_text")]
        public List<string> LogText
        {
            get => logText;
            set
            {
                logText = value;
            }
        }

        [JsonProperty("editor")]
        public string Editor { get => editor; set => editor = value; }

        [JsonProperty("editors_note")]
        public List<string> EditorsNote { get => editorsNote; set => editorsNote = value; }

        [JsonProperty("updated_at")]
        public DateTime EditedAt { get => editedAt; set => editedAt = value; }

        public void BuildDisplayText()
        {
            displayLogText = string.Join("\n", logText);
        }
    }
}