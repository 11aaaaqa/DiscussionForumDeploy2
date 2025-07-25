﻿using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UserMicroservice.Api.Controllers;
using UserMicroservice.Api.DTOs;
using UserMicroservice.Api.Models;
using UserMicroservice.Api.Services.Ban;
using UserMicroservice.Api.Services.User;

namespace UserMicroservice.UnitTests
{
    public class UserControllerTests
    {
        [Fact]
        public async Task GetUserByUserNameAsync_ReturnsOkWithUser()
        {
            string userName = It.IsAny<string>();
            var mock = new Mock<IUserService<User>>();
            mock.Setup(x => x.GetUserByUserName(userName)).ReturnsAsync(new User
            {
                Id = It.IsAny<Guid>(), Answers = It.IsAny<uint>(), UserName = userName, RegisteredAt = It.IsAny<DateOnly>(),
                Posts = It.IsAny<uint>(),BanType = It.IsAny<string>(), IsBanned = It.IsAny<bool>(),
                BannedFor = It.IsAny<string>(), BannedUntil = It.IsAny<DateTime>(), AspNetUserId = It.IsAny<string>()
            });
            var controller = new UserController(mock.Object, new Mock<IBanService<User>>().Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.GetUserByUserNameAsync(userName);

            var methodResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, methodResult.StatusCode);
            Assert.NotNull(methodResult.Value);
            var user = Assert.IsType<User>(methodResult.Value);
            Assert.Equal(userName, user.UserName);
            mock.VerifyAll();
        }

        [Fact]
        public async Task GetUserByUserNameAsync_ReturnsBadRequest()
        {
            string userName = It.IsAny<string>();
            var mock = new Mock<IUserService<User>>();
            mock.Setup(x => x.GetUserByUserName(userName)).ReturnsAsync((User?)null);
            var controller = new UserController(mock.Object, new Mock<IBanService<User>>().Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.GetUserByUserNameAsync(userName);

            Assert.IsType<BadRequestResult>(result);
            mock.VerifyAll();
        }

        [Fact]
        public async Task GetUserByIdAsync_ReturnsOkWithUser()
        {
            var userId = It.IsAny<Guid>();
            var mock = new Mock<IUserService<User>>();
            mock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(new User
            {
                Id = userId,
                Answers = It.IsAny<uint>(),
                UserName = It.IsAny<string>(),
                RegisteredAt = It.IsAny<DateOnly>(),
                Posts = It.IsAny<uint>(),
                BanType = It.IsAny<string>(),
                IsBanned = It.IsAny<bool>(),
                BannedFor = It.IsAny<string>(),
                BannedUntil = It.IsAny<DateTime>(),
                AspNetUserId = It.IsAny<string>()
            });
            var controller = new UserController(mock.Object, new Mock<IBanService<User>>().Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.GetUserByIdAsync(userId);

            var methodResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, methodResult.StatusCode);
            Assert.NotNull(methodResult.Value);
            var user = Assert.IsType<User>(methodResult.Value);
            Assert.Equal(userId, user.Id);
            mock.VerifyAll();
        }

        [Fact]
        public async Task GetUserByIdAsync_ReturnsBadRequest()
        {
            var userId = It.IsAny<Guid>();
            var mock = new Mock<IUserService<User>>();
            mock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);
            var controller = new UserController(mock.Object, new Mock<IBanService<User>>().Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.GetUserByIdAsync(userId);

            Assert.IsType<BadRequestResult>(result);
            mock.VerifyAll();
        }

        [Fact]
        public async Task IsUserBannedAsync_ReturnsOkWithFalse()
        {
            var userId = It.IsAny<Guid>();
            var mock = new Mock<IBanService<User>>();
            mock.Setup(x => x.IsUserBannedAsync(userId)).ReturnsAsync(false);
            var controller = new UserController(new Mock<IUserService<User>>().Object, mock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.IsUserBannedAsync(userId);

            var methodResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, methodResult.StatusCode);
            Assert.NotNull(methodResult.Value);
            var isBanned = Assert.IsType<bool>(methodResult.Value);
            Assert.False(isBanned);
            mock.VerifyAll();
        }

        [Fact]
        public async Task IsUserBannedAsync_ReturnsOkWithTrue()
        {
            var userId = It.IsAny<Guid>();
            var mock = new Mock<IBanService<User>>();
            mock.Setup(x => x.IsUserBannedAsync(userId)).ReturnsAsync(true);
            var controller = new UserController(new Mock<IUserService<User>>().Object, mock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.IsUserBannedAsync(userId);

            var methodResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, methodResult.StatusCode);
            Assert.NotNull(methodResult.Value);
            var isBanned = Assert.IsType<bool>(methodResult.Value);
            Assert.True(isBanned);
            mock.VerifyAll();
        }

        [Fact]
        public async Task BanUserByIdAsync_ReturnsOk()
        {
            var userId = It.IsAny<Guid>();
            var reason = It.IsAny<string>();
            var banType = It.IsAny<string>();
            var forDays = It.IsAny<uint>();
            var bannedBy = It.IsAny<string>();
            var mock = new Mock<IBanService<User>>();
            mock.Setup(x => x.BanUserAsync(userId, reason, banType, forDays, bannedBy));
            var controller = new UserController(new Mock<IUserService<User>>().Object, mock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.BanUserByIdAsync(userId,
                new BanDto { BanType = banType, ForDays = forDays, Reason = reason });

            Assert.IsType<OkResult>(result);
            mock.VerifyAll();
        }

        [Fact]
        public async Task BanUserByUserNameAsync_ReturnsOk()
        {
            var userName = It.IsAny<string>();
            var reason = It.IsAny<string>();
            var banType = It.IsAny<string>();
            var forDays = It.IsAny<uint>();
            var bannedBy = It.IsAny<string>();
            var mock = new Mock<IBanService<User>>();
            mock.Setup(x => x.BanUserAsync(userName, reason, banType, forDays, bannedBy));
            var controller = new UserController(new Mock<IUserService<User>>().Object, mock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.BanUserByUserNameAsync(userName,
                new BanDto { BanType = banType, ForDays = forDays, Reason = reason });

            Assert.IsType<OkResult>(result);
            mock.VerifyAll();
        }

        [Fact]
        public async Task UnbanUserByUserIdAsync_ReturnsOk()
        {
            var userId = It.IsAny<Guid>();
            var mock = new Mock<IBanService<User>>();
            mock.Setup(x => x.UnbanUserAsync(userId));
            var controller = new UserController(new Mock<IUserService<User>>().Object, mock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.UnbanUserByUserIdAsync(userId);

            Assert.IsType<OkResult>(result);
            mock.VerifyAll();
        }

        [Fact]
        public async Task UnbanUserByUserNameAsync_ReturnsOk()
        {
            var userName = It.IsAny<string>();
            var mock = new Mock<IBanService<User>>();
            mock.Setup(x => x.UnbanUserAsync(userName));
            var controller = new UserController(new Mock<IUserService<User>>().Object, mock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.UnbanUserByUserNameAsync(userName);

            Assert.IsType<OkResult>(result);
            mock.VerifyAll();
        }

        [Fact]
        public async Task ChangeUserNameAsync_ReturnsBadRequest()
        {
            var userId = Guid.NewGuid();
            string newUserName = It.IsAny<string>();
            var model = new ChangeUserNameDto
            {
                NewUserName = newUserName,
                UserId = userId
            };
            var mock = new Mock<IUserService<User>>();
            mock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);
            var controller = new UserController(mock.Object, new Mock<IBanService<User>>().Object, new Mock<IChangeUserName>().Object,
                new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.ChangeUserNameAsync(model);

            Assert.IsType<BadRequestResult>(result);
            mock.Verify(x => x.GetUserByIdAsync(userId));
        }
        [Fact]
        public async Task ChangeUserNameAsync_ReturnsBadRequestToo()
        {
            var userId = Guid.NewGuid();
            string newUserName = It.IsAny<string>();
            var model = new ChangeUserNameDto
            {
                NewUserName = newUserName, UserId = userId
            };
            var userMock = new Mock<IUserService<User>>();
            var changeUserNameMock = new Mock<IChangeUserName>();
            userMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(new User{UserName = It.IsAny<string>(), Id = userId});
            changeUserNameMock.Setup(x => x.ChangeUserNameAsync(userId, newUserName)).ReturnsAsync(false);
            var controller = new UserController(userMock.Object, new Mock<IBanService<User>>().Object, changeUserNameMock.Object,
                new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.ChangeUserNameAsync(model);

            Assert.IsType<BadRequestResult>(result);
            userMock.VerifyAll();
            changeUserNameMock.VerifyAll();
        }

        [Fact]
        public async Task ChangeUserNameAsync_ReturnsOk()
        {
            var userId = It.IsAny<Guid>();
            string newUserName = It.IsAny<string>();
            var model = new ChangeUserNameDto
            {
                NewUserName = newUserName,
                UserId = userId
            };
            var userMock = new Mock<IUserService<User>>();
            var changeUserNameMock = new Mock<IChangeUserName>();
            userMock.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(new User { Id = userId, UserName = It.IsAny<string>() });
            changeUserNameMock.Setup(x => x.ChangeUserNameAsync(userId, newUserName)).ReturnsAsync(true);
            var controller = new UserController(userMock.Object, new Mock<IBanService<User>>().Object, changeUserNameMock.Object,
                new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.ChangeUserNameAsync(model);

            Assert.IsType<OkResult>(result);
            userMock.VerifyAll();
            changeUserNameMock.VerifyAll();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task IsUserBannedUserNameOverload_ReturnsOk(bool isBanned)
        {
            string userName = It.IsAny<string>();
            var mock = new Mock<IBanService<User>>();
            mock.Setup(x => x.IsUserBannedAsync(userName)).ReturnsAsync(isBanned);
            var controller = new UserController(new Mock<IUserService<User>>().Object, mock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.IsUserBannedAsync(userName);

            var methodResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(methodResult.Value);
            Assert.Equal(200, methodResult.StatusCode);
            var isUserBannedResult = Assert.IsType<bool>(methodResult.Value);
            Assert.Equal(isBanned, isUserBannedResult);
            mock.VerifyAll();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task IsUserBannedAsyncByBanTypeOverloadByUserName_ReturnsOk(bool isBanned)
        {
            string[] banTypes = It.IsAny<string[]>();
            var userName = It.IsAny<string>();
            var mock = new Mock<IBanService<User>>();
            mock.Setup(x => x.IsUserBannedAsync(userName, banTypes)).ReturnsAsync(isBanned);
            var controller = new UserController(new Mock<IUserService<User>>().Object, mock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.IsUserBannedAsync(userName, banTypes);

            var methodResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(methodResult.Value);
            Assert.Equal(200, methodResult.StatusCode);
            var isUserBannedResult = Assert.IsType<bool>(methodResult.Value);
            Assert.Equal(isBanned, isUserBannedResult);
            mock.VerifyAll();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task IsUserBannedAsyncByBanTypeOverloadByUserId_ReturnsOk(bool isBanned)
        {
            string[] banTypes = It.IsAny<string[]>();
            var userId = It.IsAny<Guid>();
            var mock = new Mock<IBanService<User>>();
            mock.Setup(x => x.IsUserBannedAsync(userId, banTypes)).ReturnsAsync(isBanned);
            var controller = new UserController(new Mock<IUserService<User>>().Object, mock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.IsUserBannedAsync(userId, banTypes);

            var methodResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(methodResult.Value);
            Assert.Equal(200, methodResult.StatusCode);
            var isUserBannedResult = Assert.IsType<bool>(methodResult.Value);
            Assert.Equal(isBanned, isUserBannedResult);
            mock.VerifyAll();
        }

        [Fact]
        public async Task UserBanInfoByUserNameAsync_ReturnsOkWithNotBannedUser()
        {
            string userName = It.IsAny<string>();
            var mock = new Mock<IBanService<User>>();
            mock.Setup(x => x.IsUserBannedAsync(userName)).ReturnsAsync(false);
            var controller = new UserController(new Mock<IUserService<User>>().Object, mock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.UserBanInfoByUserNameAsync(userName);

            var methodResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(methodResult.Value);
            Assert.Equal(200, methodResult.StatusCode);
            var banInfo = Assert.IsType<UserBanInfoModel>(methodResult.Value);
            Assert.False(banInfo.IsBanned);
            Assert.Equal(banInfo.UserName, userName);
            mock.VerifyAll();
        }

        [Fact]
        public async Task UserBanInfoByUserNameAsync_ReturnsBadRequest()
        {
            string userName = It.IsAny<string>();
            var banMock = new Mock<IBanService<User>>();
            var userMock = new Mock<IUserService<User>>();
            banMock.Setup(x => x.IsUserBannedAsync(userName)).ReturnsAsync(true);
            userMock.Setup(x => x.GetUserByUserName(userName)).ReturnsAsync((User?)null);
            var controller = new UserController(userMock.Object, banMock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.UserBanInfoByUserNameAsync(userName);

            Assert.IsType<BadRequestResult>(result);
            banMock.Verify(x => x.IsUserBannedAsync(userName));
            userMock.Verify(x => x.GetUserByUserName(userName));
        }

        [Fact]
        public async Task UserBanInfoByUserNameAsync_ReturnsOkWithBannedUser()
        {
            string userName = It.IsAny<string>();
            var returnUser = new User
            {
                UserName = userName, IsBanned = true, BannedFor = It.IsAny<string>(), BanType = It.IsAny<string>(), BannedUntil = It.IsAny<DateTime>()
            };
            var banMock = new Mock<IBanService<User>>();
            var userMock = new Mock<IUserService<User>>();
            banMock.Setup(x => x.IsUserBannedAsync(userName)).ReturnsAsync(true);
            userMock.Setup(x => x.GetUserByUserName(userName)).ReturnsAsync(returnUser);
            var controller = new UserController(userMock.Object, banMock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.UserBanInfoByUserNameAsync(userName);

            var methodResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(methodResult.Value);
            Assert.Equal(200, methodResult.StatusCode);
            var banInfo = Assert.IsType<UserBanInfoModel>(methodResult.Value);
            Assert.Equal(returnUser.UserName, banInfo.UserName);
            Assert.Equal(returnUser.IsBanned, banInfo.IsBanned);
            Assert.Equal(returnUser.BannedFor, banInfo.BanReason);
            Assert.Equal(returnUser.BanType, banInfo.BanType);
            Assert.Equal(returnUser.BannedUntil, banInfo.BannedUntil);
            banMock.VerifyAll();
            userMock.VerifyAll();
        }

        [Fact]
        public async Task UserBanInfoByUserIdAsync_ReturnsOkWithNotBannedUser()
        {
            var userId = It.IsAny<Guid>();
            var mock = new Mock<IBanService<User>>();
            mock.Setup(x => x.IsUserBannedAsync(userId)).ReturnsAsync(false);
            var controller = new UserController(new Mock<IUserService<User>>().Object, mock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.UserBanInfoByUserIdAsync(userId);

            var methodResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(methodResult.Value);
            Assert.Equal(200, methodResult.StatusCode);
            var banInfo = Assert.IsType<UserBanInfoModel>(methodResult.Value);
            Assert.False(banInfo.IsBanned);
            Assert.Equal(banInfo.UserId, userId);
            mock.VerifyAll();
        }

        [Fact]
        public async Task UserBanInfoByUserIdAsync_ReturnsBadRequest()
        {
            var userId = It.IsAny<Guid>();
            var banMock = new Mock<IBanService<User>>();
            var userMock = new Mock<IUserService<User>>();
            banMock.Setup(x => x.IsUserBannedAsync(userId)).ReturnsAsync(true);
            userMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);
            var controller = new UserController(userMock.Object, banMock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.UserBanInfoByUserIdAsync(userId);

            Assert.IsType<BadRequestResult>(result);
            banMock.Verify(x => x.IsUserBannedAsync(userId));
            userMock.Verify(x => x.GetUserByIdAsync(userId));
        }

        [Fact]
        public async Task UserBanInfoByUserIdAsync_ReturnsOkWithBannedUser()
        {
            var userId = It.IsAny<Guid>();
            var returnUser = new User
            {
                Id = userId,
                IsBanned = true,
                BannedFor = It.IsAny<string>(),
                BanType = It.IsAny<string>(),
                BannedUntil = It.IsAny<DateTime>()
            };
            var banMock = new Mock<IBanService<User>>();
            var userMock = new Mock<IUserService<User>>();
            banMock.Setup(x => x.IsUserBannedAsync(userId)).ReturnsAsync(true);
            userMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(returnUser);
            var controller = new UserController(userMock.Object, banMock.Object,
                new Mock<IChangeUserName>().Object, new Mock<IPublishEndpoint>().Object, new Mock<ICheckForNormalized>().Object);

            var result = await controller.UserBanInfoByUserIdAsync(userId);

            var methodResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(methodResult.Value);
            Assert.Equal(200, methodResult.StatusCode);
            var banInfo = Assert.IsType<UserBanInfoModel>(methodResult.Value);
            Assert.Equal(returnUser.Id, banInfo.UserId);
            Assert.Equal(returnUser.IsBanned, banInfo.IsBanned);
            Assert.Equal(returnUser.BannedFor, banInfo.BanReason);
            Assert.Equal(returnUser.BanType, banInfo.BanType);
            Assert.Equal(returnUser.BannedUntil, banInfo.BannedUntil);
            banMock.VerifyAll();
            userMock.VerifyAll();
        }
    }
}
