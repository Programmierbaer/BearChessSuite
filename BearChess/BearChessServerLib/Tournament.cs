using System;
using System.Collections.Generic;

namespace www.SoLaNoSoft.com.BearChessServerLib;

public class Tournament
{
    public string Name { get; set; }
    private readonly List<TournamentGame> Games = new List<TournamentGame>();
    
    public int BoardsCount { get; set; }
    
    public bool PublishTournament { get; set; }
    
    public Guid Identifier { get; set; }

    public Tournament(Guid identifier, string name,  int boardsCount, bool publishTournament)
    {
        Identifier = identifier;
        Name = name;
        BoardsCount = boardsCount;
        PublishTournament = publishTournament;
    }

    public void AddGame(TournamentGame game)
    {
        Games.Add(game);
    }

    public void RemoveGame(TournamentGame game)
    {
        Games.Remove(game);
    }
    
    public TournamentGame[]  GetGames()
    {
        return Games.ToArray();
    }
}