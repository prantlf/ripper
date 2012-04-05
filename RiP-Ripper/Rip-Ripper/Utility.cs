// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Utility.cs" company="The Watcher">
//   Copyright (c) The Watcher Partial Rights Reserved.
//  This software is licensed under the MIT license. See license.txt for details.
// </copyright>
// <summary>
//   Code Named: RiP-Ripper
//   Function  : Extracts Images posted on RiP forums and attempts to fetch them to disk.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace RiPRipper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using System.Xml.Serialization;

    using RiPRipper.Objects;

    /// <summary>
    /// This page is probably the biggest mess I've ever managed to conceive.
    /// It's so nasty that I dare not even comment much.
    /// But as the file name says, it's just a bunch of non-dependant classes
    /// and funcs for doing nifty little things.
    /// </summary>
    public class Utility
    {
        private static readonly Configuration Conf =
            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        private static readonly AppSettingsSection App = (AppSettingsSection)Conf.Sections["appSettings"];
        

        /// <summary>
        /// Attempts to extract hotlinked and thumb-&gt;FullScale images.
        /// </summary>
        /// <param name="strDump">
        /// The STR dump.
        /// </param>
        /// <returns>
        /// The extract images.
        /// </returns>
        public static List<ImageInfo> ExtractImages(string strDump)
        {
            List<ImageInfo> rtnList = new List<ImageInfo>();
            Hashtable rtnHashChk = new Hashtable();

            try
            {
                DataSet ds = new DataSet();

                ds.ReadXml(new StringReader(strDump));

                foreach (ImageInfo newPicPool in from DataRow row in ds.Tables["Image"].Rows select new ImageInfo { ImageUrl = row["main_url"].ToString() })
                {
                    newPicPool.ImageUrl = Regex.Replace(newPicPool.ImageUrl, @"""", string.Empty);

                    //////////////////////////////////////////////////////////////////////////
                    if (IsImageNoneSense(newPicPool.ImageUrl))
                    {
                        continue;
                    }

                    newPicPool.ImageUrl = ReplaceHexWithAscii(newPicPool.ImageUrl);

                    // Remove anonym.to from Link if exists
                    if (newPicPool.ImageUrl.Contains("anonym.to"))
                    {
                        newPicPool.ImageUrl = newPicPool.ImageUrl.Replace("http://www.anonym.to/?", string.Empty);
                    }

                    // Remove redirect
                    if (newPicPool.ImageUrl.Contains("redirect-to"))
                    {
                        newPicPool.ImageUrl = newPicPool.ImageUrl.Replace("http://rip-productions.net/redirect-to/?redirect=", string.Empty);
                    }

                    // Get Real Url
                    if (newPicPool.ImageUrl.Contains("/out/out.php?x="))
                    {
                        var req = (HttpWebRequest)WebRequest.Create(newPicPool.ImageUrl);

                        req.UserAgent =
                            "User-Agent: Mozilla/5.0 (Windows; U; Windows NT 5.1; de; rv:1.8.1.1) Gecko/20061204 Firefox/2.0.0.1";
                        req.Referer = newPicPool.ImageUrl;
                        req.Timeout = 20000;

                        var res = (HttpWebResponse)req.GetResponse();

                        newPicPool.ImageUrl = res.ResponseUri.ToString();

                        res.Close();
                    }

                    if (rtnHashChk.Contains(newPicPool.ImageUrl))
                    {
                        continue;
                    }

                    rtnList.Add(newPicPool);
                    rtnHashChk.Add(newPicPool.ImageUrl, "OK");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("{0}\n{1}", ex.Message, ex.StackTrace));
            }

            return rtnList;
        }

        /// <summary>
        /// Extracts links leading to other threads and posts for indicies crawling.
        /// </summary>
        /// <param name="strDump">
        /// The STR dump.
        /// </param>
        /// <returns>
        /// The extract ri p urls.
        /// </returns>
        public static List<ImageInfo> ExtractRiPUrls(string strDump)
        {
            List<ImageInfo> rtnList = new List<ImageInfo>();
            Hashtable rtnHashChk = new Hashtable();

            try
            {
                DataSet ds = new DataSet();

                ds.ReadXml(new StringReader(strDump));

                foreach (ImageInfo newPicPool in from DataRow row in ds.Tables["Image"].Rows select new ImageInfo { ImageUrl = row["main_url"].ToString() })
                {
                    newPicPool.ImageUrl = ReplaceHexWithAscii(newPicPool.ImageUrl);

                    if (rtnHashChk.Contains(newPicPool.ImageUrl))
                    {
                        continue;
                    }

                    rtnList.Add(newPicPool);
                    rtnHashChk.Add(newPicPool.ImageUrl, "OK");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("{0}\n{1}", ex.Message, ex.StackTrace));
            }

            return rtnList;
        }

        /// <summary>
        /// Extracts the thread to posts.
        /// </summary>
        /// <param name="strDump">
        /// The STR dump.
        /// </param>
        /// <returns>
        /// The extract threadto posts.
        /// </returns>
        public static List<ImageInfo> ExtractThreadtoPosts(string strDump)
        {
            List<ImageInfo> rtnList = new List<ImageInfo>();
            Hashtable rtnHashChk = new Hashtable();

            try
            {
                DataSet ds = new DataSet();

                ds.ReadXml(new StringReader(strDump));

                foreach (ImageInfo newPicPool in ds.Tables["post"].Rows.Cast<DataRow>().Where(row => row["id"] != null).Select(row => new ImageInfo { ImageUrl = row["id"].ToString() }))
                {
                    newPicPool.ImageUrl = ReplaceHexWithAscii(newPicPool.ImageUrl);

                    if (rtnHashChk.Contains(newPicPool.ImageUrl))
                    {
                        continue;
                    }

                    rtnList.Add(newPicPool);
                    rtnHashChk.Add(newPicPool.ImageUrl, "OK");
                }
            }
            catch (Exception ex)
            {
                SaveOnCrash(strDump, ex.StackTrace, null);
            }

            return rtnList;
        }

        /// <summary>
        /// This function allows or disallows the inclusion of an image for fetching.
        /// returning true DISALLOWS the image from inclusion...
        /// </summary>
        /// <param name="szImgPth">
        /// The sz Img Pth.
        /// </param>
        /// <returns>
        /// The is image none sense.
        /// </returns>
        public static bool IsImageNoneSense(string szImgPth)
        {
            return szImgPth.ToLower().IndexOf("rip.bamva") >= 0 ||
                    (szImgPth.Contains(@"Smilies") || szImgPth.Contains(@"emoticons"));
        }

        /// <summary>
        /// Encrypts a password using MD5.
        /// not my code in this func., but falls under public domain.
        /// Author unknown. But Thanks to the author none the less.
        /// </summary>
        /// <param name="sOriginalPass">
        /// The s Original Pass.
        /// </param>
        /// <returns>
        /// The encode password.
        /// </returns>
        public static string EncodePassword(string sOriginalPass)
        {
            MD5 md5 = new MD5CryptoServiceProvider();

            byte[] originalBytes = Encoding.Default.GetBytes(sOriginalPass);
            byte[] encodedBytes = md5.ComputeHash(originalBytes);

            // Convert encoded bytes back to a 'readable' string
            return BitConverter.ToString(encodedBytes);
        }

        /// <summary>
        /// Checks if object is a Number
        /// </summary>
        /// <param name="valueToCheck">
        /// The value To Check.
        /// </param>
        /// <returns>
        /// The is numeric.
        /// </returns>
        public static bool IsNumeric(object valueToCheck)
        {
            double dummy;
            string inputValue = Convert.ToString(valueToCheck);

            bool numeric = double.TryParse(inputValue, System.Globalization.NumberStyles.Any, null, out dummy);

            return numeric;
        }

        /// <summary>
        /// It's essential to give files legal names. Otherwise the Win32API 
        /// sends back a bucket full of cow dung.
        /// </summary>
        /// <param name="sString">
        /// String to check
        /// </param>
        /// <returns>
        /// The remove illegal charecters.
        /// </returns>
        public static string RemoveIllegalCharecters(string sString)
        {
            string sNewComposed = sString;

            sNewComposed = sNewComposed.Replace("&amp;amp;", "&");
            sNewComposed = sNewComposed.Replace("\\", string.Empty);
            sNewComposed = sNewComposed.Replace("/", "-");
            sNewComposed = sNewComposed.Replace("*", "+");
            sNewComposed = sNewComposed.Replace("?", string.Empty);
            sNewComposed = sNewComposed.Replace("!", string.Empty);
            sNewComposed = sNewComposed.Replace("\"", "'");
            sNewComposed = sNewComposed.Replace("<", "(");
            sNewComposed = sNewComposed.Replace(">", ")");
            sNewComposed = sNewComposed.Replace("|", "!");
            sNewComposed = sNewComposed.Replace(":", ";");
            sNewComposed = sNewComposed.Replace("&amp;", "&");
            sNewComposed = sNewComposed.Replace("&quot;", "''");
            sNewComposed = sNewComposed.Replace("&apos;", "'");
            sNewComposed = sNewComposed.Replace("&lt;", string.Empty);
            sNewComposed = sNewComposed.Replace("&gt;", string.Empty);
            sNewComposed = sNewComposed.Replace("�", "e");
            sNewComposed = sNewComposed.Replace("\t", string.Empty);
            sNewComposed = sNewComposed.Replace("@", "at");
            sNewComposed = sNewComposed.Replace("\r", string.Empty);
            sNewComposed = sNewComposed.Replace("\n", string.Empty);

            return sNewComposed;
        }

        /// <summary>
        /// Although these are not hex, but rather html codes for special characters
        /// </summary>
        /// <param name="sURL">
        /// String to check
        /// </param>
        /// <returns>
        /// The replace hex with ascii.
        /// </returns>
        public static string ReplaceHexWithAscii(string sURL)
        {
            string sString = sURL;

            if (sString == null)
            {
                return string.Empty;
            }

            sString = sString.Replace("&amp;amp;", "&");
            sString = sString.Replace("&amp;", "&");
            sString = sString.Replace("&quot;", "''");
            sString = sString.Replace("&lt;", string.Empty);
            sString = sString.Replace("&gt;", string.Empty);
            sString = sString.Replace("�", "e");
            sString = sString.Replace("\t", string.Empty);
            sString = sString.Replace("@", "at");

            return sString;
        }

        /// <summary>
        /// This func checks to see if a file already exists at destination
        /// thats of the same name. If so, it incrementally adds numerical
        /// values prior to the image extension until the new file path doesn't
        /// already have a file there.
        /// </summary>
        /// <param name="sPath">
        /// Image path
        /// </param>
        /// <returns>
        /// The get suitable name.
        /// </returns>
        public static string GetSuitableName(string sPath)
        {
            string newAlteredPath = sPath;
            int iRenameCnt = 1;
            string sbegining = newAlteredPath.Substring(0, newAlteredPath.LastIndexOf(".", StringComparison.Ordinal));
            string sEnd = newAlteredPath.Substring(newAlteredPath.LastIndexOf(".", StringComparison.Ordinal));

            while (File.Exists(newAlteredPath))
            {
                newAlteredPath = string.Format("{0}_{1}{2}", sbegining, iRenameCnt, sEnd);
                iRenameCnt++;
            }

            return newAlteredPath;
        }

        public static SettingBase LoadSettings(SettingBase currentSettings)
        {
            var serializer = new XmlSerializer(typeof(List<SettingBase>));
            var textreader = new StreamReader(Path.Combine(Application.StartupPath, "Settings.xml"));

            var settings = (SettingBase)serializer.Deserialize(textreader);
            textreader.Close();

            return settings;
        }

        public static void SaveSettings(SettingBase currentSettings)
        {
            var serializer = new XmlSerializer(typeof(List<SettingBase>));
            var textreader = new StreamWriter(Path.Combine(Application.StartupPath, "Settings.xml"));

            serializer.Serialize(textreader, currentSettings);
            textreader.Close();
        }

        /// <summary>
        /// Loads a Setting from the App.config
        /// </summary>
        /// <param name="sKey">Setting name</param>
        /// <returns>Setting value</returns>
        public static string LoadSetting(string sKey)
        {
            string setting = App.Settings[sKey].Value;

            return setting;
        }

        /// <summary>
        /// Saves a setting to the App.config
        /// </summary>
        /// <param name="sKey">Setting Name</param>
        /// <param name="sValue">Setting Value</param>
        public static void SaveSetting(string sKey, string sValue)
        {
            if (App.Settings[sKey] != null)
            {
                App.Settings.Remove(sKey);
            }

            App.Settings.Add(sKey, sValue);

            App.SectionInformation.ForceSave = true;
            Conf.Save(ConfigurationSaveMode.Modified);

            ConfigurationManager.RefreshSection("appSettings");
        }

        /// <summary>
        /// Delete a Setting
        /// </summary>
        /// <param name="sKey">Setting Name</param>
        public static void DeleteSetting(string sKey)
        {
            if (App.Settings[sKey] != null)
            {
                App.Settings.Remove(sKey);
            }

            App.SectionInformation.ForceSave = true;
            Conf.Save(ConfigurationSaveMode.Modified);

            ConfigurationManager.RefreshSection("appSettings");
        }

        /// <summary>
        /// Gets the Security Token for the Thank You Button
        /// </summary>
        /// <param name="sURL">URL of the Post</param>
        /// <returns>The Security Token</returns>
        public static string GetSToken(string sURL)
        {
            WebClient wc = new WebClient();
            wc.Headers.Add(string.Format("Referer: {0}", sURL));
            wc.Headers.Add(
                "User-Agent: Mozilla/5.0 (Windows; U; Windows NT 5.2; en-US; rv:1.7.10) Gecko/20050716 Firefox/1.0.6");
            wc.Headers.Add(string.Format("Cookie: {0}", CookieManager.GetInstance().GetCookieString()));
            string sSToken = wc.DownloadString(sURL);

            wc.Dispose();

            const string Start = "var SECURITYTOKEN = \"";

            int iStartSrc = sSToken.IndexOf(Start);

            if (iStartSrc < 0)
            {
                return null;
            }

            iStartSrc += Start.Length;

            sSToken = sSToken.Substring(iStartSrc);

            sSToken = sSToken.Remove(sSToken.IndexOf("\";"));

            return sSToken;
        }

        /// <summary>
        /// Save all Jobs, and the current one which causes the crash to a CrashLog_...txt
        /// </summary>
        /// <param name="sExMessage">Exception Message</param>
        /// <param name="sStackTrace">Exception Stack Trace</param>
        /// <param name="mCurrentJob">Current Download Job</param>
        public static void SaveOnCrash(string sExMessage, string sStackTrace, JobInfo mCurrentJob)
        {
            const string ErrMessage =
                "An application error occurred. Please contact Admin (http://ripper.watchersnet.de/Feedback.aspx) " +
                "with the following information:";

            var currentDateTime =
                 DateTime.Now.ToString().Replace("/", string.Empty).Replace(":", string.Empty).Replace(".", string.Empty)
                     .Replace(" ", "_");

            // Save Current Job and the Error to txt file
            string sFile = string.Format("Crash_{0}.txt", currentDateTime);

            // Save Current Job and the Error to txt file
            FileStream file = new FileStream(Path.Combine(Application.StartupPath, sFile), FileMode.CreateNew);
            StreamWriter sw = new StreamWriter(file);
            sw.WriteLine(ErrMessage);
            sw.Write(sw.NewLine);
            sw.Write(sExMessage);
            sw.Write(sw.NewLine);
            sw.Write(sw.NewLine);
            sw.WriteLine("Stack Trace:");
            sw.Write(sw.NewLine);
            sw.Write(sStackTrace);
            sw.Write(sw.NewLine);
            sw.Write(sw.NewLine);

            if (mCurrentJob != null)
            {
                sw.WriteLine("Current Job DUMP:");
                sw.Write(sw.NewLine);

                sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sw.WriteLine(
                    "<ArrayOfJobInfo xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
                sw.WriteLine("  <JobInfo>");
                sw.WriteLine("    <sStorePath>{0}</sStorePath>", mCurrentJob.StorePath);
                sw.WriteLine("    <sTitle>{0}</sTitle>", mCurrentJob.Title);
                sw.WriteLine("    <sPostTitle>{0}</sPostTitle>", mCurrentJob.PostTitle);
                sw.WriteLine("    <sForumTitle>{0}</sForumTitle>", mCurrentJob.ForumTitle);
                sw.WriteLine("    <sURL>{0}</sURL>", mCurrentJob.URL);
                sw.WriteLine("    <sXMLPayLoad>{0}</sXMLPayLoad>", mCurrentJob.XMLPayLoad);
                sw.WriteLine("    <sImageCount>{0}</sImageCount>", mCurrentJob.ImageCount);
                sw.WriteLine("  </JobInfo>");
                sw.WriteLine("</ArrayOfJobInfo>");
            }

            sw.Close();
            file.Close();
        }

        /// <summary>
        /// Check the FilePath for Length because if its more then 260 characters long it will crash
        /// </summary>
        /// <param name="sFilePath">
        /// Folder Path to check
        /// </param>
        /// <returns>
        /// The check path length.
        /// </returns>
        public static string CheckPathLength(string sFilePath)
        {
            if (sFilePath.Length > 260)
            {
                string sShortFilePath = sFilePath.Substring(sFilePath.LastIndexOf("\\", StringComparison.Ordinal) + 1);

                sFilePath = Path.Combine(MainForm.userSettings.DownloadFolder, sShortFilePath);
            }

            return sFilePath;
        }
    }
}