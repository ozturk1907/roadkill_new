﻿namespace Roadkill.Text.Models
{
	/// <summary>
	/// Encapsulates the HTML returned by the markup converter, to display on the page.
	/// </summary>
	public class PageHtml
	{
		public PageHtml()
			: this("")
		{
		}

		public PageHtml(string html)
		{
			Html = html;
			IsCacheable = true;
			HeadHtml = "";
			FooterHtml = "";
			PreContainerHtml = "";
			PostContainerHtml = "";
		}

		/// <summary>
		/// The HTML to output onto the page.
		/// </summary>
		public string Html { get; set; }

		/// <summary>
		/// Whether the HTML can be cached or not. This can be set to false by plugins. True by default.
		/// </summary>
		public bool IsCacheable { get; set; }

		/// <summary>
		/// Additional HTML that should be added inside the page's head tags.
		/// </summary>
		public string HeadHtml { get; set; }

		/// <summary>
		/// Additional HTML that should be added at the bottom of the page's HTML (before the body tag)
		/// </summary>
		public string FooterHtml { get; set; }

		/// <summary>
		///  Any additional HTML that sits before the #container element.
		/// </summary>
		public string PreContainerHtml { get; set; }

		/// <summary>
		///  Any additional HTML that sits before the #container element.
		/// </summary>
		public string PostContainerHtml { get; set; }

		/// <summary>
		/// Allows a <c>PageHtml</c> to be initialized like a string.
		/// </summary>
		/// <param name="htmlValue">The HTML value</param>
		/// <returns>A PageHtml object</returns>
		public static implicit operator PageHtml(string htmlValue)
		{
			// Pagehtml p = "";
			return new PageHtml(htmlValue);
		}

		/// <summary>
		/// Allows a string to be implicitly cast from a <c>PageHtml</c>.
		/// </summary>
		/// <param name="pageHtml">A PageHtml object.</param>
		/// <returns>The PageHtml's Html property.</returns>
		public static implicit operator string(PageHtml pageHtml)
		{
			// string s = pageHtml
			return pageHtml.Html;
		}

		/// <summary>
		/// Allows a <c>PageHtml</c> to be initialized like a string.
		/// </summary>
		/// <param name="htmlValue">The HTML value</param>
		/// <returns>A PageHtml object</returns>
		public static PageHtml ToPageHtml(string htmlValue)
		{
			// Pagehtml p = "";
			return new PageHtml(htmlValue);
		}

		/// <summary>
		/// Allows a string to be implicitly cast from a <c>PageHtml</c>.
		/// </summary>
		/// <param name="pageHtml">A PageHtml object.</param>
		/// <returns>The PageHtml's Html property.</returns>
		public static string FromPageHtml(PageHtml pageHtml)
		{
			// string s = pageHtml
			return pageHtml.Html;
		}
	}
}
