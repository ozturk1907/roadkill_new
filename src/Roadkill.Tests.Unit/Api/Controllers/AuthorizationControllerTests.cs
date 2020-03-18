﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Roadkill.Api.Authorization;
using Roadkill.Api.Authorization.JWT;
using Roadkill.Api.Common.Request;
using Roadkill.Api.Common.Response;
using Roadkill.Api.Controllers;
using Roadkill.Core.Entities.Authorization;
using Shouldly;
using Xunit;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Roadkill.Tests.Unit.Api.Controllers
{
	public sealed class AuthorizationControllerTests
	{
		private readonly AuthorizationController _authorizationController;
		private readonly UserManager<RoadkillIdentityUser> _userManagerMock;
		private readonly SignInManager<RoadkillIdentityUser> _signinManagerMock;
		private readonly IJwtTokenService _jwtTokenService;

		public AuthorizationControllerTests()
		{
			var userManagerLogger = new NullLogger<UserManager<RoadkillIdentityUser>>();
			var userStoreMock = Substitute.For<IUserStore<RoadkillIdentityUser>>();
			_userManagerMock = Substitute.For<UserManager<RoadkillIdentityUser>>(
				userStoreMock,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				userManagerLogger);

			var signinManagerLogger = new NullLogger<SignInManager<RoadkillIdentityUser>>();
			var contextAccessorMock = Substitute.For<IHttpContextAccessor>();
			var claimFactoryMock = Substitute.For<IUserClaimsPrincipalFactory<RoadkillIdentityUser>>();
			_signinManagerMock = Substitute.For<SignInManager<RoadkillIdentityUser>>(
				_userManagerMock,
				contextAccessorMock,
				claimFactoryMock,
				null,
				signinManagerLogger,
				null,
				null);

			_jwtTokenService = Substitute.For<IJwtTokenService>();
			_authorizationController = new AuthorizationController(_userManagerMock, _signinManagerMock, _jwtTokenService);
		}

		[Fact]
		public void Authenticate_should_be_HttpPost_and_have_route_template()
		{
			string methodName = nameof(AuthorizationController.Authenticate);
			Type attributeType = typeof(HttpPostAttribute);

			_authorizationController.ShouldHaveAttribute(methodName, attributeType);
			_authorizationController.ShouldHaveRouteAttributeWithTemplate(methodName, methodName);
		}

		[Fact]
		public void Authenticate_should_allow_anonymous()
		{
			_authorizationController.ShouldAllowAnonymous(nameof(AuthorizationController.Authenticate));
		}

		[Fact]
		public async Task Authenticate_should_return_jwt_and_refresh_token_logging_ip()
		{
			// given
			string ipAddress = "9.8.7.6";
			string refreshToken = "refresh token";
			string jwtToken = "jwt token";
			string email = "admin@example.org";
			string password = "Passw0rd9000!";

			var roadkillUser = new RoadkillIdentityUser()
			{
				Id = "1",
				UserName = email,
				NormalizedUserName = email.ToUpperInvariant(),
				Email = email,
				NormalizedEmail = email.ToUpperInvariant(),
				RoleClaims = new List<string>()
			};

			var model = new AuthorizationRequest()
			{
				Email = email,
				Password = password
			};

			_userManagerMock.FindByEmailAsync(email)
				.Returns(Task.FromResult(roadkillUser));

			_signinManagerMock.PasswordSignInAsync(roadkillUser, password, true, false)
				.Returns(Task.FromResult(SignInResult.Success));

			var claims = new List<Claim>() { new Claim("any", "thing") } as IList<Claim>;
			_userManagerMock.GetClaimsAsync(roadkillUser)
				.Returns(Task.FromResult(claims));

			_jwtTokenService
				.CreateJwtToken(claims, roadkillUser.Email)
				.Returns(jwtToken);

			var httpContext = new DefaultHttpContext();
			httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
			_authorizationController.ControllerContext.HttpContext = httpContext;

			// TODO
			_jwtTokenService
				.StoreRefreshToken("TODO jwttoken", "TODO refresh token", email, ipAddress)
				.Returns(refreshToken);

			// when
			ActionResult<AuthorizationResponse> actionResult = await _authorizationController.Authenticate(model);

			// then
			actionResult.Result.ShouldBeOfType<OkObjectResult>();
			var okResult = actionResult.Result as OkObjectResult;
			var response = okResult.Value as AuthorizationResponse;

			response.ShouldNotBeNull();
			response.JwtToken.ShouldBe(jwtToken);
			response.RefreshToken.ShouldBe(refreshToken);
		}

		[Fact]
		public async Task Authenticate_should_return_notfound_if_user_is_not_found()
		{
			// given
			string email = "admin@example.org";
			string password = "Passw0rd9000";

			var model = new AuthorizationRequest()
			{
				Email = email,
				Password = password
			};

			_userManagerMock.FindByEmailAsync(email)
				.Returns(Task.FromResult((RoadkillIdentityUser)null));

			// when
			ActionResult<AuthorizationResponse> actionResult = await _authorizationController.Authenticate(model);

			// then
			actionResult.ShouldNotBeNull();
			actionResult.Result.ShouldBeOfType<NotFoundObjectResult>();
		}

		[Fact]
		public async Task Authenticate_should_return_forbidden_when_signin_fails()
		{
			// given
			string email = "admin@example.org";
			string password = "Passw0rd9000";
			var roadkillUser = new RoadkillIdentityUser()
			{
				Id = "1",
				UserName = email,
				NormalizedUserName = email.ToUpperInvariant(),
				Email = email,
				NormalizedEmail = email.ToUpperInvariant()
			};

			var model = new AuthorizationRequest()
			{
				Email = email,
				Password = password
			};

			_userManagerMock.FindByEmailAsync(email)
				.Returns(Task.FromResult(roadkillUser));

			_signinManagerMock.PasswordSignInAsync(roadkillUser, password, true, false)
				.Returns(Task.FromResult(SignInResult.Failed));

			// when
			ActionResult<AuthorizationResponse> actionResult = await _authorizationController.Authenticate(model);

			// then
			actionResult.ShouldNotBeNull();

			var statusCodeResult = actionResult.Result as StatusCodeResult;
			statusCodeResult.ShouldNotBeNull();
			statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
		}
	}

	// TODO
	// RefreshToken_should_return_404_when_token_not_found
	// RefreshToken_should_find_refresh_token_and_create_new_jwt_token
	// RefreshToken
	// RefreshToken

}
