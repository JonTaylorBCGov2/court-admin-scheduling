﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using CAS.API.controllers.usermanagement;
using CAS.API.models.dto;
using CAS.API.services.scheduling;
using CAS.API.services.usermanagement;
using CAS.DB.models.auth;
using tests.api.helpers;
using tests.api.Helpers;
using Xunit;
using Microsoft.Extensions.Logging;

namespace tests.controllers
{
    /// <summary>
    /// This uses CourtAdminController which is derived from UserController.
    /// This tests the more general user operations.
    /// </summary>
    public class UserControllerTests : WrapInTransactionScope
    {
        #region Variables
        private readonly CourtAdminController _controller;
        #endregion Variables

        public UserControllerTests() : base (false)
        {
            var environment = new EnvironmentBuilder("LocationServicesClient:Username", "LocationServicesClient:Password", "LocationServicesClient:Url");
            var httpContextAccessor = new HttpContextAccessor { HttpContext = HttpResponseTest.SetupHttpContext() };
            var courtAdminService = new CourtAdminService(Db, environment.Configuration, httpContextAccessor);
            var shiftService = new ShiftService(Db, courtAdminService, environment.Configuration);
            var dutyRosterService = new DutyRosterService(Db, environment.Configuration,
                shiftService, environment.LogFactory.CreateLogger<DutyRosterService>());
            _controller = new CourtAdminController(courtAdminService, dutyRosterService, shiftService, new UserService(Db), environment.Configuration, Db)
            {
                ControllerContext = HttpResponseTest.SetupMockControllerContext()
            };
        }

        [Fact]
        public async Task AssignAndUnassignRoles()
        {
            var user = await CreateUser();
            var role = await CreateRole();

            var controllerResult = await _controller.AssignRoles(new List<AssignRoleDto> {new AssignRoleDto { UserId = user.Id, RoleId = role.Id, EffectiveDate = DateTimeOffset.UtcNow }});
            HttpResponseTest.CheckForNoContentResponse(controllerResult);

            var entity = await Db.User.FindAsync(user.Id);
            Assert.True(entity.UserRoles.Count > 0);

            controllerResult = await _controller.UnassignRoles(new List<UnassignRoleDto> { new UnassignRoleDto { UserId = user.Id, RoleId = role.Id } });
            HttpResponseTest.CheckForNoContentResponse(controllerResult);

            entity = await Db.User.FindAsync(user.Id);
            Assert.True(entity.UserRoles.Count > 0);
            Assert.NotNull(entity.UserRoles.FirstOrDefault().ExpiryDate);
        }


        [Fact]
        public async Task EnableUser()
        {
            var userObject = await CreateUser();

            var controllerResult = await _controller.EnableUser(userObject.Id);
            var response = HttpResponseTest.CheckForValid200HttpResponseAndReturnValue(controllerResult);


            var dbCourtAdmin = await Db.User.FindAsync(userObject.Id);
            Assert.NotNull(dbCourtAdmin);
            Assert.True(dbCourtAdmin.IsEnabled);
        }

        [Fact]
        public async Task DisableUser()
        {
            var userObject = await CreateUser();

            var controllerResult = await _controller.DisableUser(userObject.Id);
            var response = HttpResponseTest.CheckForValid200HttpResponseAndReturnValue(controllerResult);

            var dbCourtAdmin = await Db.User.FindAsync(response.Id);
            Assert.NotNull(dbCourtAdmin);
            Assert.False(dbCourtAdmin.IsEnabled);
        }

        private async Task<Role> CreateRole()
        {
            var newRole = new Role
            {
                Description = "The big boss",
                Name = "BigBoss"
            };
            await Db.Role.AddAsync(newRole);
            await Db.SaveChangesAsync();
            return newRole;
        }

        private async Task<User> CreateUser()
        {
            var newUser = new User
            {
                FirstName = "Ted",
                LastName = "Tums",
                Email = "Ted@Teddy.com",
                IdirId = new Guid(),
                IdirName = "ted@fakeidir"
            };

            await Db.User.AddAsync(newUser);
            await Db.SaveChangesAsync();
            return newUser;
        }

    }
}
