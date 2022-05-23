namespace SpaceApple.MultiRoom
{
    public enum PvpOpCodes
    {
        LeaveGame = 0,

        Play,
        MatchFinished,

        ChangeUsername,
        ClientRoomInstantiated,
        RemovedFromRoom,
        StartTimer,
        DisplayWaitingMessage,
        PlayerCountUpdate
    }
}