using Payments.Api.Domain;

namespace Payments.Api.Tests.Domain;

public class PaymentIntentStatusMapperTests
{
    [Fact]
    public void RequiresPaymentMethod_WithoutPriorFailure_MapsToPendingConfirmation()
    {
        var status = PaymentIntentStatusMapper.Map("requires_payment_method", hasFailedConfirmationAttempt: false);

        Assert.Equal(DepositStatus.PendingConfirmation, status);
    }

    [Fact]
    public void RequiresPaymentMethod_WithPriorFailure_MapsToFailed()
    {
        var status = PaymentIntentStatusMapper.Map("requires_payment_method", hasFailedConfirmationAttempt: true);

        Assert.Equal(DepositStatus.Failed, status);
    }

    [Fact]
    public void RequiresConfirmation_MapsToPendingConfirmation()
    {
        var status = PaymentIntentStatusMapper.Map("requires_confirmation", hasFailedConfirmationAttempt: false);

        Assert.Equal(DepositStatus.PendingConfirmation, status);
    }

    [Fact]
    public void RequiresAction_MapsToActionRequired()
    {
        var status = PaymentIntentStatusMapper.Map("requires_action", hasFailedConfirmationAttempt: false);

        Assert.Equal(DepositStatus.ActionRequired, status);
    }

    [Fact]
    public void Processing_MapsToProcessing()
    {
        var status = PaymentIntentStatusMapper.Map("processing", hasFailedConfirmationAttempt: false);

        Assert.Equal(DepositStatus.Processing, status);
    }

    [Fact]
    public void Succeeded_MapsToSucceeded()
    {
        var status = PaymentIntentStatusMapper.Map("succeeded", hasFailedConfirmationAttempt: false);

        Assert.Equal(DepositStatus.Succeeded, status);
    }

    [Fact]
    public void Canceled_MapsToCancelled()
    {
        var status = PaymentIntentStatusMapper.Map("canceled", hasFailedConfirmationAttempt: false);

        Assert.Equal(DepositStatus.Cancelled, status);
    }

    [Fact]
    public void RequiresCapture_MapsToFailedAndRequiresOperationalAlert()
    {
        var status = PaymentIntentStatusMapper.Map("requires_capture", hasFailedConfirmationAttempt: false);

        Assert.Equal(DepositStatus.Failed, status);
        Assert.True(PaymentIntentStatusMapper.RequiresOperationalAlert("requires_capture"));
    }

    [Fact]
    public void OtherStatuses_DoNotRequireOperationalAlert()
    {
        Assert.False(PaymentIntentStatusMapper.RequiresOperationalAlert("succeeded"));
    }
}
