public interface IGame {
    void pauseGame();
    void resumeGame();
}

public interface IGameController {
    IGame getGame();
}
