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



        public Dictionary<String, double[]> QDict;

        private double learningRate = 0.5;
        private double discount = 0.5;
        private double epsion = 0.1;
        public double[,] qTable = new double[10, 10];


        public QLearn()
        {
            QDict = new Dictionary<String, double[]>();
        }

       
        public override int SelectMove(Cell[,] grid)
        {
            int action = greedyAction(grid);
            Random rnd = new Random();

            while (!isValid(grid,action))
            {
                action = rnd.Next(0, 7);
            }
            return action;
        }

        public double getQ(Cell[,] grid, int action)
        {
            Random rnd = new Random();
            String key = GameBoard.HashCodeToString(grid);
            if (QDict.ContainsKey(key))
            {
                return QDict[key][action];
            }
            else
            {
                double[] rndActions = new double[7];
                for (int i = 0; i < 7; i++)
                {

                    rndActions[i] = rnd.NextDouble();
                }
                QDict.Add(key, rndActions);
            }
            return 0;

        }

        public void updateQ(Cell[,] grid, int action, double qValue)
        {
            Random rnd = new Random();
            String key = GameBoard.HashCodeToString(grid);
            if (!QDict.ContainsKey(key))
            {
                double[] rndActions = new double[7];
                for (int i = 0; i < 7; i++)
                {

                    rndActions[i] = rnd.NextDouble();
                }
                QDict.Add(key, rndActions);
            }
            QDict[key][action] = qValue;
        }

        public int greedyAction(Cell[,] grid)
        {

            Random rnd = new Random();
            int action = rnd.Next(0, 7);
            if (rnd.NextDouble() < epsion)
            {

                while (grid[action, 0].Color != CellColor.Blank)
                {
                    action = rnd.Next(0, 7);
                }

            }
            else
            {
                action = maxMove(grid);
                while (grid[action, 0].Color != CellColor.Blank)
                {
                    action = rnd.Next(0, 7);
                }

            }
            return action;
        }
        public int maxMove(Cell[,] grid)
        {
            int action = 0;
            Random rnd = new Random();
            double qValue = getQ(grid, action);

            for (int i = 0; i < 7; i++)
            {
                if (getQ(grid, i) > qValue)
                {
                    action = i;
                    qValue = getQ(grid, i);
                }
            }


            bool validMove = isValid(grid, action);
            while(!validMove)
            {
                action = rnd.Next(0, 7);
                validMove = isValid(grid, action);
            }
            return action;
        }
        public bool isValid(Cell[,] grid, int col)
        {
            return grid[col, 0].Color == CellColor.Blank;
            
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
            //printBoard(qTable);
            Move move;
            int col = 0;
            int row = 0;
            for (int i = 0; i < 1000; i++)
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
                        updateQ(board.Grid, 5, 0.5);
                    }
                    else if (gameEngine.Player == CellColor.Red)
                    {
                        var test = getQ(board.Grid, 5);
                        Console.WriteLine("best action:", test);
                        if (gameEngine.Play(randomAI.SelectMove(board.Grid)))
                        {
                            updateQ(board.Grid, 5, 1);
                            //getBoard(gameEngine.Board.Grid);
                            move.MoveResult = Reward.Win;
                           // Console.WriteLine(getBoard(board, grid));
                            Console.WriteLine("win");

                        }
                        else { 
                            row = findRow(board.Grid, col);
                            //qTable[col, row] = qTable[col, row] + learningRate * (-0.1 + discount * maxMove(board.Grid, gameEngine) - qTable[col, row]);
                            continue;
                        }

                        row = findRow(board.Grid, col);
                       // qTable[col, row] = qTable[col, row] + learningRate * ((int)move.MoveResult/10 + discount * maxMove(board.Grid, gameEngine) - qTable[col, row]);
            //_matrix[cur_pos][action] = q_matrix[cur_pos][action] + learning_rate * (environment_matrix[cur_pos][action] + 
           // discount * max(q_matrix[next_state]) - q_matrix[cur_pos][action])
                    }
                    

                    else if(gameEngine.Player == CellColor.Yellow)
                    {
                        if (gameEngine.Play(randomAI.SelectMove(board.Grid)))
                        {
                            updateQ(board.Grid, 5, -1);
                            move.MoveResult = Reward.Loss;
                            Console.WriteLine("loss");
                        }

                    }
                    

                }
               
            }


            //printBoard(qTable);
        }

        
        public override int SelectMove(Cell[,] grid)
        {
            throw new NotImplementedException();
        }
    }

}
