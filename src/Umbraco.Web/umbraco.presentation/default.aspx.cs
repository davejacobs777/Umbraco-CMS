﻿using System;
using System.Threading;
using System.Web;
using System.Web.Routing;
using System.Web.UI;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using Umbraco.Core.Logging;
using Umbraco.Web;
using Umbraco.Web.Routing;
using Umbraco.Core.Configuration;
using Umbraco.Core.IO;
using umbraco.cms.businesslogic.web;
using umbraco.cms.businesslogic;

namespace umbraco
{
	/// <summary>
	/// The codebehind class for the main default.aspx page that does the webforms rendering in Umbraco
	/// </summary>
	/// <remarks>
	/// We would move this to the UI project but there is a public API property and some protected properties which people may be using so 
	/// we cannot move it.
	/// </remarks>
	public class UmbracoDefault : Page
	{
		private page _upage = null;
		private DocumentRequest _docRequest = null;
		bool _validateRequest = true;

		/// <summary>
		/// To turn off request validation set this to false before the PageLoad event. This equivalent to the validateRequest page directive
		/// and has nothing to do with "normal" validation controls. Default value is true.
		/// </summary>
		public bool ValidateRequest
		{
			get { return _validateRequest; }
			set { _validateRequest = value; }
		}

		// fixme - switch over to OnPreInit override
		void Page_PreInit(Object sender, EventArgs e)
		{

			// get the document request and the page
			_docRequest = UmbracoContext.Current.DocumentRequest;
			_upage = _docRequest.GetUmbracoPage();							
			var templatePath = SystemDirectories.Masterpages + "/" + _docRequest.Template.Alias.Replace(" ", "") + ".master"; // fixme - should be in .Template!
			this.MasterPageFile = templatePath; // set the template
				
			// reset the friendly path so it's used by forms, etc.			
			Context.RewritePath(UmbracoContext.Current.RequestUrl.PathAndQuery);
		}

		//SD: I'm nearly positive that this is taken care of in our DefaultLastChanceLookup class!

		//void OnPreInitLegacy()
		//{
		//    if (_upage.Template == 0)
		//    {
		//        string custom404 = umbraco.library.GetCurrentNotFoundPageId();
		//        if (!String.IsNullOrEmpty(custom404))
		//        {
		//            XmlNode xmlNodeNotFound = content.Instance.XmlContent.GetElementById(custom404);
		//            if (xmlNodeNotFound != null)
		//            {
		//                _upage = new page(xmlNodeNotFound);
		//            }
		//        }
		//    }

		//    if (_upage.Template != 0)
		//    {
		//        this.MasterPageFile = template.GetMasterPageName(_upage.Template);

		//        string cultureAlias = null;
		//        for (int i = _upage.SplitPath.Length - 1; i > 0; i--)
		//        {
		//            var domains = Domain.GetDomainsById(int.Parse(_upage.SplitPath[i]));
		//            if (domains.Length > 0)
		//            {
		//                cultureAlias = domains[0].Language.CultureAlias;
		//                break;
		//            }
		//        }

		//        if (cultureAlias != null)
		//        {
		//            LogHelper.Debug<UmbracoDefault>("Culture changed to " + cultureAlias, Context.Trace);
		//            var culture = new System.Globalization.CultureInfo(cultureAlias);
		//            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture = culture;
		//        }
		//    }
		//    else
		//    {
		//        Response.StatusCode = 404;
		//        RenderNotFound();
		//        Response.End();
		//    }
		//}

		//
		
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (ValidateRequest)
				Request.ValidateInput();
			// handle the infamous umbDebugShowTrace, etc
			Page.Trace.IsEnabled &= GlobalSettings.DebugMode && !String.IsNullOrWhiteSpace(Request["umbDebugShowTrace"]);
		}

		//
		protected override void Render(HtmlTextWriter writer)
		{
			// do the original rendering
			TextWriter sw = new StringWriter();
			base.Render(new HtmlTextWriter(sw));
			string text = sw.ToString();

			// filter / parse internal links - although this should be done elsewhere!
			text = template.ParseInternalLinks(text);

			// filter / add preview banner
			if (UmbracoContext.Current.InPreviewMode)
			{
				LogHelper.Debug<UmbracoDefault>("Umbraco is running in preview mode.", Context.Trace);

				if (Response.ContentType == "text/HTML") // ASP.NET default value
				{
					int pos = text.ToLower().IndexOf("</body>");
					if (pos > -1)
					{
						string htmlBadge =
							String.Format(UmbracoSettings.PreviewBadge,
								IOHelper.ResolveUrl(SystemDirectories.Umbraco),
								IOHelper.ResolveUrl(SystemDirectories.UmbracoClient),
								Server.UrlEncode(UmbracoContext.Current.HttpContext.Request.Path));

						text = text.Substring(0, pos) + htmlBadge + text.Substring(pos, text.Length - pos);
					}
				}
			}

			// render
			writer.Write(text);
		}

		////TODO: This should be removed, we should be handling all 404 stuff in the module and executing the 
		//// DocumentNotFoundHttpHandler instead but we need to fix the above routing concerns so that this all
		//// takes place in the Module.
		//void RenderNotFound()
		//{
		//    Context.Response.StatusCode = 404;

		//    Response.Write("<html><body><h1>Page not found</h1>");
		//    UmbracoContext.Current.HttpContext.Response.Write("<h3>No umbraco document matches the url '" + HttpUtility.HtmlEncode(UmbracoContext.Current.ClientUrl) + "'.</h3>");

		//    // fixme - should try to get infos from the DocumentRequest?

		//    Response.Write("<p>This page can be replaced with a custom 404. Check the documentation for \"custom 404\".</p>");
		//    Response.Write("<p style=\"border-top: 1px solid #ccc; padding-top: 10px\"><small>This page is intentionally left ugly ;-)</small></p>");
		//    Response.Write("</body></html>");
		//}
	}
}
