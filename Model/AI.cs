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
        public enum Reward : int
        {
            InPlay = 0,
            Loss = -10,
            Win = 10,
            IsValid = -1,
            Draw = 5
           
        }
        public struct Move
        {
            public int Index;
            public Reward MoveResult;
        }


       
        public Dictionary<String, double[]> getQDict;

        private double learningRate = 0.5;
        private double discount = 0.5;

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
                    Console.Write(temp[i, j]);
                }
                Console.WriteLine();
            }
            return temp;
        }
        public void printBoard(double[,] grid)
        {         
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 6; j++)
                {    
                    Console.Write(grid[i, j].ToString("F3") + " ");
                }
                Console.WriteLine();
            }
            
        }
        public double maxMove(Cell[,] grid, GameEngine engine)
        {
            double q = 0;

            Cell[,] temp = grid;
            GameEngine tempEninge = engine;

            for (int i = 0; i < 7; i++)
            {
                
                tempEninge.Play(temp,i, CellColor.Yellow);
                for (int j = 0; j < 7; j++)
                {
                    if (qTable[i, j] > q)
                        q = qTable[i, j]; 
                    
                }
               temp = grid;
            }

            return q;
        }
        
        public int findRow(Cell[,] grid, int col)
        {
            for (int i = 5; i >= 0; i--)
            {
                if (grid[col, i].Color == CellColor.Blank)
                {
                    return i+1;
                }   
            }

            return 0;
        }
        
        public void playGames(Cell[,] grid)
        {
            var randomAI = new RandomAI();
            printBoard(qTable);
            Move move;
            int col = 0;
            int row = 0;
            for (int i = 0; i < 5000; i++)
            {
                move.MoveResult = Reward.InPlay;
                GameBoard board = new GameBoard();
                GameEngine gameEngine = new GameEngine();
                Console.WriteLine(i);
                

                while (move.MoveResult == Reward.InPlay)
                {
                    if (gameEngine.IsDraw())
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
                        else { 
                            row = findRow(board.Grid, col);
                            qTable[col, row] = qTable[col, row] + learningRate * (-0.1 + discount * maxMove(board.Grid, gameEngine) - qTable[col, row]);
                            continue;
                        }

                        row = findRow(board.Grid, col);
                        qTable[col, row] = qTable[col, row] + learningRate * ((int)move.MoveResult/10 + discount * maxMove(board.Grid, gameEngine) - qTable[col, row]);
            //_matrix[cur_pos][action] = q_matrix[cur_pos][action] + learning_rate * (environment_matrix[cur_pos][action] + 
           // discount * max(q_matrix[next_state]) - q_matrix[cur_pos][action])
                    }
                    

                    else if(gameEngine.Player == CellColor.Yellow)
                    {
                        if (gameEngine.Play(randomAI.SelectMove(board.Grid)))
                        {
                            move.MoveResult = Reward.Loss;
                            Console.WriteLine("loss");
                        }

                    }
                    

                }
               
            }


            printBoard(qTable);
        }

        
        public override int SelectMove(Cell[,] grid)
        {
            throw new NotImplementedException();
        }
    }

}
