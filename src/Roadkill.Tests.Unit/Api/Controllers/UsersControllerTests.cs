﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Roadkill.Api.Authorization;
using Roadkill.Api.Authorization.JWT;
using Roadkill.Api.Authorization.Policies;
using Roadkill.Api.Common.Request;
using Roadkill.Api.Common.Response;
using Roadkill.Api.Controllers;
using Roadkill.Api.ObjectConverters;
using Roadkill.Core.Entities.Authorization;
using Shouldly;
using Xunit;

namespace Roadkill.Tests.Unit.Api.Controllers
{
	public sealed class UsersControllerTests
	{
		private readonly Fixture _fixture;
		private readonly UsersController _usersController;
		private readonly UserManager<RoadkillIdentityUser> _userManagerMock;
		private readonly IUserObjectsConverter _objectConverterMock;

		public UsersControllerTests()
		{
			_fixture = new Fixture();
			var fakeStore = Substitute.For<IUserStore<RoadkillIdentityUser>>();

			_userManagerMock = Substitute.For<UserManager<RoadkillIdentityUser>>(
				fakeStore,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				new NullLogger<UserManager<RoadkillIdentityUser>>());

			_objectConverterMock = Substitute.For<IUserObjectsConverter>();
			_usersController = new UsersController(_userManagerMock, _objectConverterMock);
		}

		[Fact]
		public void All_methods_should_disallow_anonymous()
		{
			Type controllerType = typeof(UsersController);

			foreach (MethodInfo methodInfo in controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
			{
				_usersController.ShouldNotAllowAnonymous(methodInfo.Name);
			}
		}

		[Theory]
		[InlineData(nameof(UsersController.Get), PolicyNames.GetUser, "{email}")]
		[InlineData(nameof(UsersController.FindAll), PolicyNames.FindUsers)]
		[InlineData(nameof(UsersController.FindUsersWithClaim), PolicyNames.FindUsers)]
		public void FindAndGet_methods_should_be_HttpGet_with_custom_routeTemplate_and_authorize_policy(string methodName, string policyName, string routeTemplate = "")
		{
			Type attributeType = typeof(HttpGetAttribute);
			if (string.IsNullOrEmpty(routeTemplate))
			{
				routeTemplate = methodName;
			}

			_usersController.ShouldHaveAttribute(methodName, attributeType);
			_usersController.ShouldHaveRouteAttributeWithTemplate(methodName, routeTemplate);
			_usersController.ShouldAuthorizePolicy(methodName, policyName);
		}

		[Fact]
		public void Add_should_be_HttpPost()
		{
			string methodName = nameof(UsersController.CreateAdmin);
			Type attributeType = typeof(HttpPostAttribute);

			_usersController.ShouldHaveAttribute(methodName, attributeType);
		}

		[Fact]
		public void Delete_should_be_HttpDelete()
		{
			string methodName = nameof(UsersController.Delete);
			Type attributeType = typeof(HttpDeleteAttribute);

			_usersController.ShouldHaveAttribute(methodName, attributeType);
		}

		[Fact]
		public async Task Get_should_return_user()
		{
			// given
			string email = "donny@trump.com";
			var identityUser = new RoadkillIdentityUser() { Email = email };
			var responseUser = new UserResponse() { Email = email };

			_userManagerMock.FindByEmailAsync(email)
				.Returns(identityUser);

			_objectConverterMock
				.ConvertToUserResponse(Arg.Any<RoadkillIdentityUser>())
				.Returns(responseUser);

			// when
			var actionResult = await _usersController.Get(email);

			// then
			actionResult.ShouldBeOkObjectResult();
			UserResponse actualUser = actionResult.GetOkObjectResultValue();
			actualUser.Email.ShouldBe(email);
		}

		[Fact]
		public async Task Get_should_return_notfound_when_user_doesnt_exist()
		{
			// given
			string email = "okfingers@trump.com";
			var expectedError = UsersController.EmailDoesNotExistError;

			_userManagerMock.FindByEmailAsync(email)
				.Returns(Task.FromResult((RoadkillIdentityUser)null));

			// when
			ActionResult<UserResponse> actionResult = await _usersController.Get(email);

			// then
			actionResult.ShouldBeNotFoundObjectResult();
			string errorMessage = actionResult.GetNotFoundValue<UserResponse, string>();
			errorMessage.ShouldBe(expectedError);
		}

		[Fact]
		public void FindAll_should_return_all_users_from_manager()
		{
			// given
			var expectedAllUsers = _fixture.CreateMany<RoadkillIdentityUser>(5);
			_userManagerMock.Users.Returns(expectedAllUsers.AsQueryable());

			// when
			var actionResult = _usersController.FindAll();

			// then
			actionResult.ShouldBeOkObjectResult();
			IEnumerable<UserResponse> actualUsers = actionResult.GetOkObjectResultValue();
			actualUsers.Count().ShouldBe(expectedAllUsers.Count());
		}

		[Fact]
		public async Task FindUsersWithClaim_should_return_specific_users_for_claim()
		{
			// given
			string claimName = ClaimTypes.Role;
			string claimValue = RoadkillClaims.AdminClaim.Value;

			var expectedUsers = new List<RoadkillIdentityUser>()
			{
				_fixture.Create<RoadkillIdentityUser>(),
				_fixture.Create<RoadkillIdentityUser>()
			};

			_userManagerMock
				.GetUsersForClaimAsync(Arg.Is<Claim>(c =>
					c.Type == claimName && c.Value == claimValue))
				.Returns(Task.FromResult((IList<RoadkillIdentityUser>)expectedUsers));

			// when
			var actionResult = await _usersController.FindUsersWithClaim(claimName, claimValue);

			// then
			actionResult.ShouldBeOkObjectResult();
			IEnumerable<UserResponse> actualUsers = actionResult.GetOkObjectResultValue();
			actualUsers.Count().ShouldBe(2);
		}

		[Fact]
		public async Task CreateAdmin_should_create_user_with_usermanager_and_add_claim()
		{
			// given
			string email = "donald@trump.com";
			string password = "fakepassword";

			_userManagerMock.CreateAsync(
					Arg.Is<RoadkillIdentityUser>(
						u => u.Email == email &&
							 u.EmailConfirmed &&
							 u.UserName == email), password)
				.Returns(Task.FromResult(IdentityResult.Success));

			var requestModel = new UserRequest()
			{
				Email = email,
				Password = password
			};

			// when
			ActionResult<string> actionResult = await _usersController.CreateAdmin(requestModel);

			// then
			actionResult.ShouldBeCreatedAtActionResult();
			string actualEmailAddress = actionResult.CreatedAtActionResultValue();
			actualEmailAddress.ShouldBe(email);
		}

		[Fact]
		public async Task CreateAdmin_should_return_badrequest_if_email_exists()
		{
			// given
			string email = "donald@trump.com";
			string password = "fakepassword";
			var expectedError = UsersController.EmailExistsError;

			_userManagerMock.FindByEmailAsync(email)
				.Returns(new RoadkillIdentityUser() { Email = email });

			var requestModel = new UserRequest()
			{
				Email = email,
				Password = password
			};

			// when
			var actionResult = await _usersController.CreateAdmin(requestModel);

			// then
			actionResult.ShouldBeBadRequestObjectResult();
			string errorMessage = actionResult.GetBadRequestValue();
			errorMessage.ShouldBe(expectedError);
		}

		[Fact]
		public async Task CreateEditor_should_create_user_with_usermanager_and_add_claim()
		{
			// given
			string email = "daffy@trump.com";
			string password = "fakepassword";

			_userManagerMock.CreateAsync(
					Arg.Is<RoadkillIdentityUser>(
						u => u.Email == email &&
							 u.EmailConfirmed &&
							 u.UserName == email), password)
				.Returns(Task.FromResult(IdentityResult.Success));

			var requestModel = new UserRequest()
			{
				Email = email,
				Password = password
			};

			// when
			var actionResult = await _usersController.CreateEditor(requestModel);

			// then
			actionResult.ShouldBeCreatedAtActionResult();
			string actualEmailAddress = actionResult.CreatedAtActionResultValue();
			actualEmailAddress.ShouldBe(email);
		}

		[Fact]
		public async Task CreateEditor_should_return_badrequest_if_email_exists()
		{
			// given
			string email = "daffy@trump.com";
			string password = "fakepassword";
			var expectedError = UsersController.EmailExistsError;

			_userManagerMock.FindByEmailAsync(email)
				.Returns(new RoadkillIdentityUser() { Email = email });

			var requestModel = new UserRequest()
			{
				Email = email,
				Password = password
			};

			// when
			var actionResult = await _usersController.CreateEditor(requestModel);

			// then
			actionResult.ShouldBeBadRequestObjectResult();
			string errorMessage = actionResult.GetBadRequestValue();
			errorMessage.ShouldBe(expectedError);
		}

		[Fact]
		public async Task DeleteUser_should_return_no_content_and_set_user_as_locked_out()
		{
			// given
			string email = "okfingers@trump.com";

			_userManagerMock.FindByEmailAsync(email)
				.Returns(new RoadkillIdentityUser() { Email = email });

			_userManagerMock.UpdateAsync(Arg.Is<RoadkillIdentityUser>(u => u.Email == email))
				.Returns(Task.FromResult(IdentityResult.Success));

			// when
			ActionResult<string> actionResult = await _usersController.Delete(email);

			// then
			actionResult.ShouldBeNoContentResult();
		}

		[Fact]
		public async Task DeleteUser_should_return_notfound_when_user_doesnt_exist()
		{
			// given
			string email = "fakeuser@trump.com";
			var expectedError = UsersController.EmailDoesNotExistError;

			_userManagerMock.FindByEmailAsync(email)
				.Returns(Task.FromResult((RoadkillIdentityUser)null));

			// when
			var actionResult = await _usersController.Delete(email);

			// then
			actionResult.ShouldBeNotFoundObjectResult();
			string errorMessage = actionResult.GetNotFoundValue();
			errorMessage.ShouldBe(expectedError);
		}

		[Fact]
		public async Task DeleteUser_should_return_badrequest_when_user_is_already_lockedout()
		{
			// given
			string email = "lockedout@trump.com";
			var expectedError = UsersController.UserIsLockedOutError;

			_userManagerMock.FindByEmailAsync(email)
				.Returns(new RoadkillIdentityUser()
				{
					Email = email,
					LockoutEnd = DateTime.MaxValue,
					LockoutEnabled = true
				});

			// when
			var actionResult = await _usersController.Delete(email);

			// then
			actionResult.ShouldBeBadRequestObjectResult();
			string errorMessage = actionResult.GetBadRequestValue();
			errorMessage.ShouldBe(expectedError);
		}
	}
}
