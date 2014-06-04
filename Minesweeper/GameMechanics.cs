using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Minesweeper
{
    public enum CellContent { FiredMine = 0, Mine = 1, Unknown = 2, Flag = 3, WrongFlag = 4, Number = 5, Count = 14};
    public enum ActionResult { Continuation = 0, Win = 1, Losing = 2};
    public enum PerformedAction { Open, Falg, MineDetector, BadAction };

    public class Error : Exception { }

    public class Cell
    {
        public Cell()
        {
            opened = false;
            marked = false;
            number = -3;
        }

        public void makeFiredMine() { number = -2; }

        public void makeMine() { number = -1; }

        [XmlIgnore]
        public bool isFiredMine { get { return number == -2; } }

        [XmlIgnore]
        public bool isMine { get { return number == -1; } }

        [XmlIgnore]
        public bool isNumber { get { return number >= 0; } }
        
        /*  Values description
         *  -3 - initial value, 
         *  -2 - fired mine, 
         *  -1 - mine, 
         *   0 - empty, 
         *   1, 2, 3,... - displayed numbers*/
        public int number { get; set; }

        public bool opened { get; set; }

        public bool marked { get; set; }
    }

    public class GameMechanics
    {
        public GameMechanics(int width, int height, int numberOfMines)
        {            
            if (width <= 0 || height <= 0)
                throw new Error();

            this.width = width;

            if (numberOfMines >= height * width || numberOfMines <= 0)
                throw new Error();

            if (height > 100 || width > 100)
                throw new Error();

            cells = new Cell[height * width];
            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                    cells[i * width + j] = new Cell();

            this.numberOfMines = numberOfMines;

            wasInitialized = false;
        }

        public GameMechanics() {}

        public void Initialize(int init_i, int init_j)
        {
            Random rand = new Random();

            int init_pos = init_i * width + init_j;

            int actualNumberOfMines = 0;
            while (actualNumberOfMines < numberOfMines)
            {
                int pos = rand.Next(height * width);

                if (!cells[pos].isMine && pos != init_pos)
                {
                    cells[pos].makeMine();
                    actualNumberOfMines++;
                }
            }

            for (int center_i = 0; center_i < height; center_i++)
                for (int center_j = 0; center_j < width; center_j++)
                    if (!cells[center_i * width + center_j].isMine)
                    {
                        cells[center_i * width + center_j].number = 0;
                        for (int a = -1; a < 2; a++)
                            for (int b = -1; b < 2; b++)
                            {
                                if (a == 0 && b == 0) continue;

                                Cell neighbor = TakeFrom(center_i, center_j, a, b);
                                if (neighbor != null && neighbor.isMine)
                                    cells[center_i * width + center_j].number++;
                            }
                    }

            wasInitialized = true;
        }

        public ActionResult TakeAction(int i, int j, Microsoft.Xna.Framework.Input.Touch.GestureType gesture, out PerformedAction performedAction)
        {
            performedAction = PerformedAction.BadAction;

            if (i < 0 || i >= height || j < 0 || j >= width)
                return ActionResult.Continuation;

            Cell cell = cells[i * width + j];

            if (cell.number > 0 && cell.opened && gesture == Microsoft.Xna.Framework.Input.Touch.GestureType.Tap) performedAction = PerformedAction.MineDetector;
            if (!cell.opened && gesture == Microsoft.Xna.Framework.Input.Touch.GestureType.Tap) performedAction = PerformedAction.Falg;
            if (!cell.marked && !cell.opened && gesture == Microsoft.Xna.Framework.Input.Touch.GestureType.Hold) performedAction = PerformedAction.Open;

            return TakeAction(i, j, performedAction);
        }

        public CellContent GetCellContent(int i, int j, out int number)
        {
            Cell cell = cells[i * width + j];
            number = 0;

            if (cell.opened)
            {
                if (cell.isFiredMine) return CellContent.FiredMine;
                if (cell.isMine && !cell.marked) return CellContent.Mine;
                if (cell.isMine && cell.marked) return CellContent.Flag;
                if (!cell.isMine && cell.marked) return CellContent.WrongFlag;
                if (cell.isNumber)
                {
                    number = cell.number;
                    return CellContent.Number;
                }
            }
            else
            {
                if (cell.marked) return CellContent.Flag;
                if (!cell.marked) return CellContent.Unknown;
            }

            return CellContent.Count;
        }

        public int number_of_flags
        {
            get 
            {
                int flags = 0;
                foreach (Cell cell in cells)
                {
                    if (cell.marked) flags++;
                }
                return flags;
            }
        }

        [XmlIgnore]
        public int height
        {
            get { return cells.Length / width; }
        }

        public bool wasInitialized { get; set; }
        
        public int numberOfMines { get; set; }
        
        public int width {get; set;}

        public Cell[] cells { get; set; }        

        private void Explosion(int i, int j)
        {
            Cell cell = cells[i * width + j];
            cell.makeFiredMine();
            cell.opened = true;

            for (int a = 0; a < height; a++)
            {
                for (int b = 0; b < width; b++)
                {
                    if (cells[a * width + b].isMine || cells[a * width + b].marked)
                        cells[a * width + b].opened = true;
                }
            }
        }        

        private ActionResult TakeAction(int i, int j, PerformedAction action)
        {
            if (action == PerformedAction.BadAction)
                return ActionResult.Continuation;

            Cell cell = cells[i * width + j];

            switch (action)
            {
                case PerformedAction.Falg:
                    if (!cell.opened)
                        cell.marked = !cell.marked;
                    break;
                case PerformedAction.Open:
                    if (!cell.marked)
                    {
                        if (!wasInitialized)
                            Initialize(i, j);

                        if (cell.isMine)
                        {
                            Explosion(i, j);
                            return ActionResult.Losing;
                        }

                        if (!cell.opened)
                        {
                            Exapnd(i, j);
                        }
                    }
                    break;

                case PerformedAction.MineDetector:
                    if (!cell.marked)
                    {
                        int mistakes = 0,
                            unmarked = 0,
                            unmarked_i = -1,
                            unmarked_j = -1;

                        for (int a = -1; a < 2; a++)
                        {
                            for (int b = -1; b < 2; b++)
                            {
                                if (a == 0 && b == 0) continue;
                                Cell neighbor = TakeFrom(i, j, a, b);
                                if (neighbor != null && neighbor.marked && !neighbor.isMine) mistakes++;
                                if (neighbor != null && !neighbor.marked && neighbor.isMine)
                                {
                                    unmarked_i = i + a;
                                    unmarked_j = j + b;
                                    unmarked++;
                                }
                            }
                        }

                        if (mistakes > 0 && unmarked > 0)
                        {
                            Explosion(unmarked_i, unmarked_j);
                            return ActionResult.Losing;
                        }

                        if (mistakes == 0 && unmarked == 0)
                        {
                            for (int a = -1; a < 2; a++)
                            {
                                for (int b = -1; b < 2; b++)
                                {
                                    if (a == 0 && b == 0) continue;
                                    Cell neighbor = TakeFrom(i, j, a, b);
                                    if (neighbor != null && !neighbor.marked)
                                        Exapnd(i + a, j + b);
                                }
                            }
                        }
                    }
                    break;
            }

            if (!wasInitialized)
                return ActionResult.Continuation;

            for (int center_i = 0; center_i < height; center_i++)
            {
                for (int center_j = 0; center_j < width; center_j++)
                {
                    if (cells[center_i * width + center_j].isNumber && !cells[center_i * width + center_j].opened)
                        return ActionResult.Continuation;
                }
            }

            return ActionResult.Win;

        }

        private void Exapnd(int i, int j)
        {
            cells[i * width + j].opened = true;

            if (cells[i * width + j].number == 0)
            {
                for (int a = -1; a < 2; a++)
                    for (int b = -1; b < 2; b++)
                    {
                        Cell neighbor = TakeFrom(i, j, a, b);
                        if (neighbor != null && !neighbor.isMine && !neighbor.opened && !neighbor.marked)
                            Exapnd(i + a, j + b);
                    }
            }
        }

        private Cell TakeFrom(int center_i, int center_j, int move_i, int move_j)
        {
            int new_i = center_i + move_i,
                new_j = center_j + move_j;

            if (new_i >= 0 && new_i < height && new_j >= 0 && new_j < width)
                return cells[new_i * width + new_j];

            return null;
        }       
    }
}
