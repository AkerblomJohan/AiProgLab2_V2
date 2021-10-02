﻿using System;
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
        private double epsion = 0.9;
       

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

        public static QLearn ConstructFromFile(string fileName)
        {
            QLearn temp = (QLearn)(AI.FromFile(fileName));
          
            return temp;
        }



        public double qValueNextState(Cell[,] grid, GameEngine engine)
        {
            Cell[,] copyGrid = grid.Clone() as Cell[,];

            
           
            Random rnd = new Random();
            int action = rnd.Next(0, 7);
            while (!isValid(grid, action))
                action = rnd.Next(0, 7);

            if (engine.Play(grid, action, CellColor.Yellow))
            {

                int bestAction = greedyAction(copyGrid);
                return getQ(copyGrid, bestAction);
            }



            return 0;
            
        }
        public void playGames()
        {
            var randomAI = new RandomAI();
            Move move;
 
            int action= 0;
            int wins = 0;
            int loss = 0;
            int draw = 0;
            for (int i = 0; i < 10000; i++)
            {
                move.MoveResult = Reward.InPlay;
                //GameBoard board = new GameBoard();
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
                    else if (gameEngine.Player == CellColor.Red)
                    {
                        double qValue = getQ(gameEngine.Board.Grid, action);
                        double qValueNext = qValueNextState(gameEngine.Board.Grid, gameEngine);
                        updateQ(gameEngine.Board.Grid, action, (qValue + alpha * (gamma * qValueNext - qValue)));
                        
                        action = greedyAction(gameEngine.Board.Grid);
                        
                        if (gameEngine.Play(action))
                        {
                            updateQ(gameEngine.Board.Grid, action, 1);
                           
                            move.MoveResult = Reward.Win;

                            wins++;

                        }

                    }
                    else if(gameEngine.Player == CellColor.Yellow)
                    {
                        
                            if (gameEngine.Play(randomAI.SelectMove(gameEngine.Board.Grid)))
                            {
                                updateQ(gameEngine.Board.Grid, action, -1);
                                move.MoveResult = Reward.Loss;
                           
                                loss++;
                            }

                    }

                }
               
            }

            Console.WriteLine("wins " + wins);

            Console.WriteLine("loss " + loss);

            Console.WriteLine("draw " + draw);
        }
        
    }
}
