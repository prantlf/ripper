//////////////////////////////////////////////////////////////////////////
// Code Named: RiP-Ripper
// Function  : Extracts Images posted on RiP forums and attempts to fetch
//			   them to disk.
//
// This software is licensed under the MIT license. See license.txt for
// details.
// 
// Copyright (c) The Watcher
// Partial Rights Reserved.
// 
//////////////////////////////////////////////////////////////////////////
// This file is part of the RiP Ripper project base.

using System;
using System.Collections;
using System.Web;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;

namespace RiPRipper
{
    using RiPRipper.Objects;

    /// <summary>
	/// Worker class for UploadImages.net
	/// </summary>
	public class uploadimages_net : ServiceTemplate
	{
		public uploadimages_net(ref string sSavePath, ref string strURL, ref Hashtable hTbl)
			: base( sSavePath, strURL, ref hTbl )
		{
		}

		protected override bool DoDownload()
		{
			string strImgURL = mstrURL;

			if (eventTable.ContainsKey(strImgURL))	
			{
				return true;
			}

			string strFilePath = string.Empty;

			
			strFilePath = strImgURL.Substring(  strImgURL.IndexOf( "img=" ) + 4 );

            try
            {
                if (!Directory.Exists(mSavePath))
                    Directory.CreateDirectory(mSavePath);
            }
            catch (IOException ex)
            {
                MainForm.DeleteMessage = ex.Message;
                MainForm.Delete = true;

                return false;
            }

            strFilePath = Path.Combine(mSavePath, Utility.RemoveIllegalCharecters(strFilePath));

			CacheObject CCObj = new CacheObject();
			CCObj.IsDownloaded = false;
			CCObj.FilePath = strFilePath ;
			CCObj.Url = strImgURL;
			try
			{
				eventTable.Add(strImgURL, CCObj);
			}
			catch (ThreadAbortException)
			{
				return true;
			}
			catch(System.Exception)
			{
				if (eventTable.ContainsKey(strImgURL))	
				{
					return false;
				}
				else
				{
					eventTable.Add(strImgURL, CCObj);
				}
			}
			
			string strIVPage = GetImageHostPage(ref strImgURL);

			if (strIVPage.Length < 10)
			{
				return false;
			}

			string strNewURL = string.Empty;


			int iStartIMG = 0;
			int iEndSRC = 0;
			iStartIMG = strIVPage.IndexOf("<img src=\"" + strNewURL);

			if (iStartIMG < 0)
			{
				return false;
			}
			iStartIMG += 10;

			iEndSRC = strIVPage.IndexOf("\"/>", iStartIMG);

			if (iEndSRC < 0)
			{
				return false;
			}
			
			strNewURL = strIVPage.Substring(iStartIMG, iEndSRC - iStartIMG);
			
			//////////////////////////////////////////////////////////////////////////
			string NewAlteredPath = Utility.GetSuitableName(strFilePath);
			if (strFilePath != NewAlteredPath)
			{
				strFilePath = NewAlteredPath;
				((CacheObject)eventTable[mstrURL]).FilePath = strFilePath;
			}

			FileStream lFileStream = new FileStream(strFilePath, FileMode.Create);

			
			//int bytesRead;

			try
			{
                WebClient client = new WebClient();
                client.Headers.Add("Referer: " + strImgURL);
                client.Headers.Add("User-Agent: Mozilla/5.0 (Windows; U; Windows NT 5.2; en-US; rv:1.7.10) Gecko/20050716 Firefox/1.0.6");
                client.DownloadFile(strNewURL, strFilePath);
                client.Dispose();
            }
            catch (ThreadAbortException)
            {
                ((CacheObject)eventTable[strImgURL]).IsDownloaded = false;
                ThreadManager.GetInstance().RemoveThreadbyId(mstrURL);

                return true;
            }
            catch (IOException ex)
            {
                MainForm.DeleteMessage = ex.Message;
                MainForm.Delete = true;

                ((CacheObject)eventTable[strImgURL]).IsDownloaded = false;
                ThreadManager.GetInstance().RemoveThreadbyId(mstrURL);

                return true;
            }
            catch (WebException)
            {
                ((CacheObject)eventTable[strImgURL]).IsDownloaded = false;
                ThreadManager.GetInstance().RemoveThreadbyId(mstrURL);

                return false;
            }

            ((CacheObject)eventTable[mstrURL]).IsDownloaded = true;
            //CacheController.GetInstance().u_s_LastPic = ((CacheObject)eventTable[mstrURL]).FilePath;
            CacheController.GetInstance().uSLastPic =((CacheObject)eventTable[mstrURL]).FilePath = strFilePath;

			return true;
		}

	}
}
