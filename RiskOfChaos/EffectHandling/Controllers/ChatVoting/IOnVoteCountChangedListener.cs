namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public interface IOnVoteCountChangedListener
    {
        void OnVoteCountChanged(int newVoteCount);
    }
}
