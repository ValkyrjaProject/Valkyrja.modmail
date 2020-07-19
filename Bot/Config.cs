using System;

using guid = System.UInt64;

namespace Valkyrja.modmail
{
	public class Config: Valkyrja.entities.BaseConfig
	{
		public guid ModmailServerId = 0;
		public guid ModmailCategoryId = 0;
		public guid ModmailArchiveCategoryId = 0;
		public bool ModmailUseEmbeds = true;
		public string ModmailFooterOverride = "";
	}
}
