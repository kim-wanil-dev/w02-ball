using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    TutorialBasic,
    CinematicIntro,
    TutorialPuzzle,
    Playing,
    CinematicEnding

}

public struct SpawnPoint
{
    public Vector3 Position;
    public Quaternion Rotation;

    public SpawnPoint(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }
}

public class GameManager
{
    private GameState _gameState;
    private readonly List<GameState> _stateOrder = new()
    {
        GameState.TutorialBasic,
        GameState.CinematicIntro,
        GameState.TutorialPuzzle,
        GameState.Playing,
        GameState.CinematicEnding
    };
    private readonly Dictionary<GameState, SpawnPoint> _spawnPoint;

    private GameObject _prizeUI;

    public List<GameState> StateOrder { get { return _stateOrder; } }
    public Dictionary<GameState, SpawnPoint> SpawnPoint { get { return _spawnPoint; } }
    public GameObject PrizeUI { get { return _prizeUI; } }

    public void Init()
    {
        _gameState = _stateOrder[0];
        _prizeUI = Resources.Load<GameObject>("Prefabs/UIs/PrizeCanvas");
    }

    public void MoveToNextState()
    {
        int index = _stateOrder.IndexOf(_gameState);

        if (index >= _stateOrder.Count - 1)
            return;

        _gameState = _stateOrder[index + 1];
    }

    public void Clear()
    {
        _gameState = _stateOrder[0];
    }

    public GameState CheckPlaying()
    {
        return _gameState;
    }
}