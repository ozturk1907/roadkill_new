﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roadkill.Api.Common.Models;
using Roadkill.Api.JWT;
using Roadkill.Api.ModelConverters;
using Roadkill.Core.Entities;
using Roadkill.Core.Repositories;

namespace Roadkill.Api.Controllers
{
	[Authorize]
	[ApiController] // [ApiController] adds [FromBody] by default and model validation
	[ApiVersion("3")]
	[Route("v{version:apiVersion}/[controller]")]
	public class PagesController : ControllerBase
	{
		private readonly IPageRepository _pageRepository;

		private readonly IPageModelConverter _pageModelConverter;

		public PagesController(IPageRepository pageRepository, IPageModelConverter pageModelConverter)
		{
			_pageRepository = pageRepository;
			_pageModelConverter = pageModelConverter;
		}

		/// <summary>
		/// Gets a single page by its ID.
		/// </summary>
		/// <param name="id">The unique ID of the page to retrieve.</param>
		/// <returns>The page meta information, or a 404 not found if the page cannot be found.
		/// No page text is returned, use PageVersions to get this information.</returns>
		[HttpGet]
		[AllowAnonymous]
		[Route("{id}")]
		public async Task<ActionResult<PageModel>> Get(int id)
		{
			Page page = await _pageRepository.GetPageByIdAsync(id);
			if (page == null)
			{
				return NotFound();
			}

			return _pageModelConverter.ConvertToViewModel(page);
		}

		/// <summary>
		/// Retrieves all pages in the Roadkill database.
		/// </summary>
		/// <returns>Meta information for all the pages in the database.
		/// No page text is returned, use PageVersions to get this information.</returns>
		[HttpGet]
		[Route(nameof(AllPages))]
		[AllowAnonymous]
		public async Task<ActionResult<IEnumerable<PageModel>>> AllPages()
		{
			IEnumerable<Page> allpages = await _pageRepository.AllPagesAsync();
			return Ok(allpages.Select(_pageModelConverter.ConvertToViewModel));
		}

		/// <summary>
		/// Retrieves all pages created by a particular user.
		/// </summary>
		/// <param name="username">The username (typically an email address) of the user that created
		/// the the pages.</param>
		/// <returns>Meta information for all the pages created by the user in the database.
		/// No page text is returned, use PageVersions to get this information.</returns>
		[HttpGet]
		[Route(nameof(AllPagesCreatedBy))]
		[AllowAnonymous]
		public async Task<ActionResult<IEnumerable<PageModel>>> AllPagesCreatedBy(string username)
		{
			IEnumerable<Page> pagesCreatedBy = await _pageRepository.FindPagesCreatedByAsync(username);

			IEnumerable<PageModel> models = pagesCreatedBy.Select(_pageModelConverter.ConvertToViewModel);
			return Ok(models);
		}

		/// <summary>
		/// Finds the first page in the database with the "homepage" tag.
		/// </summary>
		/// <returns>The page meta information, or a 404 not found if the page cannot be found.
		/// No page text is returned, use PageVersions to get this information.</returns>
		[HttpGet]
		[Route(nameof(FindHomePage))]
		[AllowAnonymous]
		public async Task<ActionResult<PageModel>> FindHomePage()
		{
			IEnumerable<Page> pagesWithHomePageTag = await _pageRepository.FindPagesContainingTagAsync("homepage");

			if (!pagesWithHomePageTag.Any())
			{
				return NotFound();
			}

			Page firstResult = pagesWithHomePageTag.First();
			return _pageModelConverter.ConvertToViewModel(firstResult);
		}

		/// <summary>
		/// Finds the first page matching the given page title.
		/// </summary>
		/// <param name="title">The title of the page to search for (case-insensitive).</param>
		/// <returns>The page meta information, or a 404 not found if the page cannot be found.
		/// No page text is returned, use PageVersions to get this information.</returns>
		[HttpGet]
		[Route(nameof(FindByTitle))]
		[AllowAnonymous]
		public async Task<ActionResult<PageModel>> FindByTitle(string title)
		{
			Page page = await _pageRepository.GetPageByTitleAsync(title);
			if (page == null)
			{
				return NotFound();
			}

			return _pageModelConverter.ConvertToViewModel(page);
		}

		/// <summary>
		/// Add a page to the database using the provided meta information. This will only add
		/// the meta information not the page text, use PageVersions to add text for a page.
		/// </summary>
		/// <param name="model">The page information to add.</param>
		/// <returns>A 202 HTTP status with the newly created page, with its generated ID populated.</returns>
		[HttpPost]
		[Authorize(Policy = PolicyNames.Editor)]
		public async Task<ActionResult<PageModel>> Add([FromBody] PageModel model)
		{
			// TODO: add base62 ID, as Id in Marten is Hilo and starts at 1000 as the lo
			// TODO: fill createdon property
			// TODO: validate
			// http://www.anotherchris.net/csharp/friendly-unique-id-generation-part-2/
			Page page = _pageModelConverter.ConvertToPage(model);
			if (page == null)
			{
				return NotFound();
			}

			Page newPage = await _pageRepository.AddNewPageAsync(page);
			PageModel newModel = _pageModelConverter.ConvertToViewModel(newPage);

			return CreatedAtAction(nameof(Add), nameof(PagesController), newModel);
		}

		/// <summary>
		/// Updates an existing page in the database.
		/// </summary>
		/// <param name="model">The page details to update, which should include the page id.</param>
		/// <returns>The update page details, or a 404 not found if the existing page cannot be found</returns>
		[HttpPut]
		[Authorize(Policy = PolicyNames.Editor)]
		public async Task<ActionResult<PageModel>> Update(PageModel model)
		{
			Page page = _pageModelConverter.ConvertToPage(model);
			if (page == null)
			{
				return NotFound();
			}

			Page newPage = await _pageRepository.UpdateExistingAsync(page);
			return _pageModelConverter.ConvertToViewModel(newPage);
		}

		/// <summary>
		/// Deletes an existing page from the database. This is an administrator-only action.
		/// </summary>
		/// <param name="pageId">The id of the page to remove.</param>
		/// <returns>A 200 OK if the page successfully deleted.</returns>
		[HttpDelete]
		[Authorize(Policy = PolicyNames.Admin)]
		public async Task Delete(int pageId)
		{
			await _pageRepository.DeletePageAsync(pageId);
		}
	}
}
