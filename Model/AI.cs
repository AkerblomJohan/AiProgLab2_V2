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
            temp.generator = new Random();
            return temp;
        }
    }

    [Serializable]
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

        private double alpha = 0.5; 
        private double gamma = 0.9; 
        private double epsilon = 0.9;
       
        //Dictionary for qvalues
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


        //Fill values with random numbers if it does not exist yet otherwise gets the qvalues from the dictioniary that is declared.
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

        //Updates the qvalues in the qdict otherwise random numbers as was mentioned in the getQ function.
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


        //Epsilon greedy action 
        public int greedyAction(Cell[,] grid)
        {

            Random rnd = new Random();
            int action = rnd.Next(0, 7);
            if (rnd.NextDouble() < epsilon)
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

        //Best action for the ai
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

        //Checks if it is a valid move/action that is being performed
        public bool isValid(Cell[,] grid, int col)
        {
            return grid[col, 0].Color == CellColor.Blank;
            
        }

        public static QLearn FileConstructor(string fileName)
        {
            QLearn temp = (QLearn)(AI.FromFile(fileName));
          
            return temp;
        }

        private bool Play(Cell[,] grid, int action)  //Kommentera bort?
        {
            return true;
        }

        public bool IsDraw(Cell[,] grid)
        {
            for (int i = 0; i < 7; i++)
            {
                if (grid[i, 0].Color == CellColor.Blank)
                {
                    return false;
                }
            }
            return true;
        }
        private CellColor otherColor(CellColor color)
        {
            if (color == CellColor.Red)
            {
                return CellColor.Yellow;
            }
            else
            {
                return CellColor.Red;
            }
        }


        //Copies the board that is in play so that the ai knows what the best move is in the next action.
        public double qValueNextState(Cell[,] grid, GameEngine engine, CellColor color)
        {

            Cell[,] copy = engine.copyBoard(grid);
            
            Random rnd = new Random();
            int action = rnd.Next(0, 7);
            if (!engine.IsDraw())
            {
                
                while (!isValid(copy, action))
                    action = rnd.Next(0, 7);
                
                engine.Play(copy, action, otherColor(color));
                
                if (!IsDraw(copy))
                {
                    
                    action = greedyAction(copy);
                  
                    return getQ(copy, action);
                }
            }
            return getQ(copy, action);
        }

        public double[,] getBoard(Cell[,] grid)
        {
            double[,] temp = new double[7, 6];
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    if (grid[i, j].Color == CellColor.Blank)
                        temp[i, j] = 0;
                    if (grid[i, j].Color == CellColor.Red)
                        temp[i, j] = 1;
                    if (grid[i, j].Color == CellColor.Yellow)
                        temp[i, j] = 2;

                }
                
            }
            return temp;
        }
        public void printBoard(double[,] grid)
        {
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    Console.Write(grid[i, j]);//.ToString("F3") + " ");
                }
                Console.WriteLine();
            }

        }


        //train the agents
        public void playGames(CellColor colorToTrain)
        {
            var randomAI = new RandomAI();
            Move move;
 
            int action= 0;
            int wins = 0;
            int loss = 0;
            int draw = 0;
            for (int i = 0; i < 500; i++)
            {
                move.MoveResult = Reward.InPlay;
                
                GameEngine gameEngine = new GameEngine();
                Console.WriteLine(i);
                
                
                while (move.MoveResult == Reward.InPlay)
                {
                    if (gameEngine.IsDraw())
                    {
                        
                        move.MoveResult = Reward.Draw;
                        draw++;
                        updateQ(gameEngine.Board.Grid, action, 0.5);
                        
                    }
                    else if (gameEngine.Player == colorToTrain)
                    {
                        
                        double qValue = getQ(gameEngine.Board.Grid, action);
                       
                        double qValueNext = qValueNextState(gameEngine.Board.Grid, gameEngine, colorToTrain);
                       
                        updateQ(gameEngine.Board.Grid, action, (qValue + alpha * (gamma * qValueNext - qValue)));
                       
                        action = greedyAction(gameEngine.Board.Grid);
                       
                        if (gameEngine.Play(action))
                        {
                            updateQ(gameEngine.Board.Grid, action, 1);
                           
                            move.MoveResult = Reward.Win;

                            wins++;

                        }

                    }
                    else
                    {
                        
                        if (gameEngine.Play(randomAI.SelectMove(gameEngine.Board.Grid)))
                            {
                                updateQ(gameEngine.Board.Grid, action, -1);
                                move.MoveResult = Reward.Loss;
                           
                                loss++;
                            }

                    }

                }
               // printBoard(getBoard( gameEngine.Board.Grid));


            }

            Console.WriteLine("wins " + wins);

            Console.WriteLine("loss " + loss);

            Console.WriteLine("draw " + draw);
        }
        
    }
}
