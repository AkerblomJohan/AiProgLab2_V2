using System;
using System.IO;
using BlazorConnect4.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
namespace BlazorConnect4.AIModels
{
    [Serializable]
    public abstract class AI
    {
        // Funktion för att bestämma vilken handling som ska genomföras.
        public abstract int SelectMove(Cell[,] grid);

        // Funktion för att skriva till fil.
        public virtual void ToFile(string fileName)
        {
            using (Stream stream = File.Open(fileName, FileMode.Create))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bformatter.Serialize(stream, this);
            }
        }

        // Funktion för att att läsa från fil.
        protected static AI FromFile(string fileName)
        {
            AI returnAI;
            using (Stream stream = File.Open(fileName, FileMode.Open))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                returnAI = (AI)bformatter.Deserialize(stream);
            }
            return returnAI;

        }

    }


    [Serializable]
    public class RandomAI : AI
    {
        [NonSerialized] Random generator;

        public RandomAI()
        {
            generator = new Random();
        }

        public override int SelectMove(Cell[,] grid)
        {
           
            return generator.Next(7);
        }

        public static RandomAI ConstructFromFile(string fileName)
        {
            RandomAI temp = (RandomAI)(AI.FromFile(fileName));
            // Eftersom generatorn inte var serialiserad.
            temp.generator = new Random();
            return temp;
        }
    }

    
    public class QLearn : AI
    {
        public enum Reward
        {
            InPlay = 0,
            Loss = -1,
            Win = 1,
            Draw = 5
           
        }
        public struct Move
        {
            public int Index;
            public Reward MoveResult;
        }


       
        public Dictionary<String, double[]> getQDict;

        private double learningRate = 5;
        private double discount = 5;

        public int GetBestAction(Cell[,] state)
        {
            String key = GameBoard.HashCodeToString(state);
            
            getQDict = new Dictionary<String, double[]>();
            Console.WriteLine("Dic : ",getQDict);
            int action = 0;
            double value = getQDict[key][0];
            for (int i = 1; i < 7; i++)
            {
                if (getQDict[key][i] > value)
                {
                    action = i;
                    value = getQDict[key][i];
                    Console.WriteLine("key is : ", value);
                }
            }
            return action;
        }

        public int[,] getBoard(Cell[,] grid)
        {
            int[,] temp = new int[7,6];
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    if (grid[i, j].Color == CellColor.Red)
                        temp[i, j] = 1;
                    if (grid[i, j].Color == CellColor.Yellow)
                        temp[i, j] = 2;
                    if (grid[i, j].Color == CellColor.Blank)
                        temp[i, j] = 0;
                }
            }
            return temp;
        }
        
        public void playGames(Cell[,] grid)
        {
            var randomAI = new RandomAI();
            Move move;  
            for (int i = 0; i < 1; i++)
            {
                move.MoveResult = Reward.InPlay;
                GameBoard board = new GameBoard();
                GameEngine gameEngine = new GameEngine();
                Console.WriteLine(i);

                while (move.MoveResult == Reward.InPlay)
                {
                    
                    Console.WriteLine(grid);
                    if(gameEngine.IsDraw())
                    {
                        Console.WriteLine("draw");
                        move.MoveResult = Reward.Draw;
                        
                    }
                    else if (gameEngine.Player == CellColor.Red)
                    {
                        var test = GetBestAction(board.Grid);
                        Console.WriteLine("best action:", test);
                        if (gameEngine.Play(randomAI.SelectMove(board.Grid)))
                        {
                            getBoard(gameEngine.Board.Grid);
                            move.MoveResult = Reward.Win;
                           // Console.WriteLine(getBoard(board, grid));
                            Console.WriteLine("win");

                        }
                     
                           
                        
                    }
                    else if (gameEngine.Player == CellColor.Yellow)
                    {
                        if (gameEngine.Play(randomAI.SelectMove(board.Grid)))
                        {
                            move.MoveResult = Reward.Loss;
                            Console.WriteLine("loss");
                        }

                    }
                }
               
            }

        }
        
      
        
        public override int SelectMove(Cell[,] grid)
        {
            throw new NotImplementedException();
        }
    }

}
