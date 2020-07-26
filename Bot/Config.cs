using System;

using guid = System.UInt64;

namespace Valkyrja.modmail
{
	public class Config: Valkyrja.entities.BaseConfig
	{
		public guid ModmailServerId = 0;
		public guid ModmailCategoryId = 0;
		public guid ModmailArchiveCategoryId = 0;
		public int ModmailArchiveLimit = 5;
		//public bool ModmailUseEmbeds = true;
		public string ModmailFooterOverride = "";
		public string ModmailNewThreadMessage = "";
		public string ModmailEmbedColorAdmins = "#ff0000";
		public string ModmailEmbedColorMods = "#0000ff";
		public string ModmailEmbedColorMembers = "#00ff00";
	}
}
