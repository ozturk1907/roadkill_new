﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace Roadkill.Api.Controllers
{
	[Route("[controller]")]
	[SwaggerIgnore]
	public class ErrorController : Controller
	{
		public IActionResult Index()
		{
			var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
			return Json(feature.Error.ToString());
		}
	}
}