using FluentAssertions;
using TourDuLich.Application.Services;

namespace TourDuLich.IntegrationTests;

public sealed class YeuCauThietKeStateMachineTests
{
    [Fact]
    public void CancelAndChoose_AreOnlyAllowedFromNewRequest()
    {
        YeuCauThietKeStateMachine.CanCancel(YeuCauThietKeStateMachine.Moi).Should().BeTrue();
        YeuCauThietKeStateMachine.CanCancel(YeuCauThietKeStateMachine.DangThietKe).Should().BeFalse();
        YeuCauThietKeStateMachine.CanChooseProposal(YeuCauThietKeStateMachine.Huy, false).Should().BeFalse();
        YeuCauThietKeStateMachine.CanChooseProposal(YeuCauThietKeStateMachine.Moi, true).Should().BeFalse();
    }

    [Fact]
    public void ProposalGenerationAndApproval_RequireExactStates()
    {
        YeuCauThietKeStateMachine.CanGenerateProposals(YeuCauThietKeStateMachine.Huy).Should().BeFalse();
        YeuCauThietKeStateMachine.CanSubmitForApproval(
            YeuCauThietKeStateMachine.DangThietKe,
            YeuCauThietKeStateMachine.Nhap).Should().BeTrue();
        YeuCauThietKeStateMachine.CanApprove(
            YeuCauThietKeStateMachine.ChoDuyet,
            YeuCauThietKeStateMachine.ChoXacNhan).Should().BeTrue();
        YeuCauThietKeStateMachine.CanApprove(
            YeuCauThietKeStateMachine.CanChinhSua,
            YeuCauThietKeStateMachine.Nhap).Should().BeFalse();
    }
}
