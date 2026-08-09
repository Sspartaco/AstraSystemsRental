using System.Net;
using AstraSystemsRental.Mail.Api.Dtos;
using AstraSystemsRental.Mail.Api.Services;
using FluentAssertions;
using Moq;

namespace AstraSystemsRental.Mail.Api.Tests;

public class MailServiceTests
{
    private static MailService CreateService(Mock<ISmtpSender> sender, Mock<ITemplateRenderer> renderer)
    {
        renderer.Setup(r => r.RenderAsync(It.IsAny<string>(), It.IsAny<WelcomeModel>()))
            .ReturnsAsync("<html>rendered</html>");
        return new MailService(renderer.Object, sender.Object);
    }

    [Fact(DisplayName = "SendWelcome renders template and sends the email on valid request")]
    public async Task SendWelcome_ValidRequest_SendsEmail()
    {
        // Arrange
        var sender = new Mock<ISmtpSender>();
        var renderer = new Mock<ITemplateRenderer>();
        var service = CreateService(sender, renderer);
        var request = new SendWelcomeRequest("user@codalea.app", "Jane Doe", "https://astra.local/confirm?token=abc");

        // Act
        var result = await service.SendWelcomeAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        sender.Verify(s => s.SendAsync("user@codalea.app", It.IsAny<string>(), "<html>rendered</html>", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "SendWelcome fails with 400 when email is invalid")]
    public async Task SendWelcome_InvalidEmail_Fails()
    {
        // Arrange
        var sender = new Mock<ISmtpSender>();
        var renderer = new Mock<ITemplateRenderer>();
        var service = CreateService(sender, renderer);
        var request = new SendWelcomeRequest("not-an-email", "Jane", "https://astra.local/confirm?token=abc");

        // Act
        var result = await service.SendWelcomeAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        sender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "SendWelcome fails when confirmation url is empty")]
    public async Task SendWelcome_EmptyUrl_Fails()
    {
        // Arrange
        var sender = new Mock<ISmtpSender>();
        var renderer = new Mock<ITemplateRenderer>();
        var service = CreateService(sender, renderer);
        var request = new SendWelcomeRequest("user@codalea.app", "Jane", "");

        // Act
        var result = await service.SendWelcomeAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("ConfirmationUrl"));
    }
}
