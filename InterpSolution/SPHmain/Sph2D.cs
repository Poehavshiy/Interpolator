using SimpleIntegrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using static System.Math;

namespace SPH_2D {
    /// <summary>
    /// Интегратор, представляющий двумерную задачу SPH метода
    /// В качестве частицы выступает интерфейс IParticle2D
    /// </summary>
    public class Sph2D: ScnObjDummy, IScnObj {
        #region local classes

        /// <summary>
        /// Класс представляющий плоскую ячейку с частицами (необходим для быстрого и параллельного определения соседей частиц)
        /// </summary>
        class Cell2D {

            /// <summary>
            /// Частицы, находящиеся в ячейке
            /// </summary>
            public List<IParticle2D> Particles = new List<IParticle2D>();


            /// <summary>
            /// Стек с частицами, добавленными в ячейку асинхронными методом. Необходимо перенести их в Particles методом DumpStack()
            /// </summary>
            public ConcurrentStack<IParticle2D> ParticleStack = new ConcurrentStack<IParticle2D>();

            /// <summary>
            /// Индексы ячейки в сетке ячеек
            /// </summary>
            public int XInd, YInd;

            /// <summary>
            /// Границы ячейки.
            /// Левая и нижняя границы включены в пространство ячейки,
            /// Правая и верхняя границы не включены в пространство ячейки
            /// </summary>
            public double X0, X1, Y0, Y1;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="XInd">Индекс колонки ячейки в сетке ячеек</param>
            /// <param name="YInd">Индекс строки ячейки в сетке ячеек</param>
            /// <param name="X0">Левая граница (включена в пространство ячейки)</param>
            /// <param name="X1">Правая граница (включена в пространство ячейки)</param>
            /// <param name="Y0">Нижняя граница (не включена в пространство ячейки)</param>
            /// <param name="Y1">Верхняя граница(не включена в пространство ячейки)</param>
            public Cell2D(int XInd,int YInd,double X0,double X1,double Y0,double Y1) {
                this.XInd = XInd;
                this.YInd = YInd;
                this.X0 = X0;
                this.X1 = X1;
                this.Y0 = Y0;
                this.Y1 = Y1;
            }

            /// <summary>
            /// Подходит ли частица к этой ячейке?
            /// Находится ли частица в пространстве ячейки?
            /// </summary>
            /// <param name="particle"></param>
            /// <returns></returns>
            bool GoodParticle(IParticle2D particle) {
                return !(particle.X < X0 || particle.X >= X1
                       || particle.Y < Y0 || particle.Y >= Y1);
            }

            /// <summary>
            /// Перенести частицы из стэка в Лист с частицами
            /// </summary>
            public void DumpStack() {
                Particles.AddRange(ParticleStack);
                ParticleStack.Clear();

            }

            /// <summary>
            /// Очистить лист с частицами от неподходящих частиц,
            /// неподходяцие частицы переносятся в ConcurrentStack<IParticle2D> toThis
            /// </summary>
            /// <param name="toThis">Push'ит неподходяцие частицы сюда</param>
            public void DumpBadParticles(ConcurrentStack<IParticle2D> toThis) {
                var badParticles = Particles.Where(p => !GoodParticle(p)).ToArray();
                if(badParticles.Length == 0)
                    return;
                toThis.PushRange(badParticles);
                Particles = Particles.Except(badParticles).ToList();
            }

            /// <summary>
            /// Заполнить поле Neibs у каждой частицы, находящейся в ячейке
            /// В качестве соседей для частицы выступают ВСЕ частицы, находящиеся в этой и в соседних ячейках (за исключением самой частицы)
            /// </summary>
            /// <param name="cells"></param>
            public void FillAllNeibs(Cell2D[,] cells) {
                int i0 = XInd - 1 >= 0 ? XInd - 1 : XInd;
                int j0 = YInd - 1 >= 0 ? YInd - 1 : YInd;
                int i1 = XInd + 1 < cells.GetLength(0) ? XInd + 1 : XInd;
                int j1 = YInd + 1 < cells.GetLength(1) ? YInd + 1 : YInd;
                var allPart = Enumerable.Empty<IParticle2D>();
                
                for(int i = i0; i <= i1; i++) {
                    for(int j = j0; j <= j1; j++) {
                        allPart = allPart.Concat(cells[i,j].Particles);
                    }
                }
                var allPlst = allPart.ToList();
                foreach(var particle in Particles) {
                    particle.Neibs = allPlst;
                }
            }
        }
        #endregion

        #region private fields

        /// <summary>
        /// Собственно матрица ячеек 
        /// [индекс по Х, индекс по Y]
        /// [столбец, строка]
        /// Размерность [Ncols, Nrows]
        /// </summary>
        Cell2D[,] CellNet;

        /// <summary>
        /// Кол-во строк и столбцов в матрице ячеек
        /// </summary>
        int Nrows, Ncols;

        /// <summary>
        /// Они же, но только в списке (для параллельного перебора)
        /// </summary>
        List<Cell2D> Cells = new List<Cell2D>();

        /// <summary>
        /// Характеристики сетки ячеек
        /// </summary>
        double 
            hmax, //максимальная длина сглаживания, она же является стороной квадратной ячейки
            xmin, //левая минимальная координата сетки (она же минимальная Х координата частиц)
            xmax, //правая максимальная координата сетки (она же максимальная Х координата частиц)
            ymin, //нижняя минимальная координата сетки (она же минимальная Y координата частиц)
            ymax; //верхнияя максимальная координата сетки (она же максимальная Y координата частиц)

        /// <summary>
        /// Тут лежат частицы, которым нужно распределение по ячейкам
        /// </summary>
        ConcurrentStack<IParticle2D> LostPatricleStack = new ConcurrentStack<IParticle2D>();
        #endregion

        #region public fields

        /// <summary>
        /// Лист c интегрируемыми частицами (добавляются в Children)
        /// </summary>
        public List<IParticle2D> Particles = new List<IParticle2D>();

        /// <summary>
        /// Лист со всеми частицами 
        /// </summary>
        public List<IParticle2D> AllParticles = new List<IParticle2D>();

        /// <summary>
        /// Лист с частицами, представляющими "стены"
        /// </summary>
        public List<IParticle2D> WallParticles = new List<IParticle2D>();

        public int MaxStuffCount { get; private set; }

        public double border = 10;

        #endregion

        #region private methods
        /// <summary>
        /// "пересобрать ячейки"
        /// </summary>
        void RebuildCells() {
            if(CellNet!=null)
            foreach(var cell in CellNet) {
                cell.Particles.Clear();
            }
            CellNet = null;
            Cells.Clear();
            if(AllParticles.Count == 0)
                return;
            xmin = AllParticles.Min(p => p.X) - hmax * border;
            ymin = AllParticles.Min(p => p.Y) - hmax * border;
            xmax = AllParticles.Max(p => p.X) + hmax * border;
            ymax = AllParticles.Max(p => p.Y) + hmax * border;

            Nrows = (int)Ceiling((ymax - ymin) / hmax);
            Ncols = (int)Ceiling((xmax - xmin) / hmax);
            CellNet = new Cell2D[Ncols,Nrows];
            for(int i = 0; i < Ncols; i++) {
                for(int j = 0; j < Nrows; j++) {
                    double x0 = xmin + i * hmax;
                    double x1 = x0 + hmax;
                    double y0 = ymin + j * hmax;
                    double y1 = y0 + hmax;
                    CellNet[i,j] = new Cell2D(i,j,x0,x1,y0,y1);
                    Cells.Add(CellNet[i,j]);
                }
            }
        }
      
        /// <summary>
        /// Заполнить ячейки
        /// </summary>
        public void FillCells() {
            //Оставляем только хорошие частицы, остальное в Lost
            Parallel.ForEach(Cells.Where(c=>c.Particles.Count>0),c => {
                c.DumpBadParticles(LostPatricleStack);
            });

            //Распределяем Lost по ячейкам
            Parallel.ForEach(LostPatricleStack,p => {
                int xind = GetHashXInd(p);
                int yind = GetHashYInd(p);
                if(xind<0 || xind >= Ncols || yind < 0 || yind >= Nrows) {
                    int j = 0;
                }
                CellNet[xind,yind].ParticleStack.Push(p);
            });
            LostPatricleStack.Clear();

            //Оставляем только хорошие частицы, остальное в Lost
            Parallel.ForEach(Cells.Where(c => c.ParticleStack.Count > 0),c => {
                c.DumpStack();
            });
        }

        /// <summary>
        /// Заполняем соседей
        /// </summary>
        public void FillNeibs() {
            Parallel.ForEach(Cells.Where(c => c.Particles.Count > 0),c => {
                c.FillAllNeibs(CellNet);
            });
        }

        /// <summary>
        /// Вызываем методы DoStuff(1)...DoStuff(n) у ВСЕХ частиц 
        /// </summary>
        void UpdateParticles() {
            //Заполняем d/dt
            for(int i = 0; i < MaxStuffCount; i++) {
                Parallel.ForEach(Particles.Where(p => i < p.StuffCount),p => {
                    p.DoStuff(i);
                });

                //foreach(var p in AllParticles.Where(p => i < p.StuffCount)) {
                //    p.DoStuff(i);

                //}
            }
        }

        /// <summary>
        /// ScnObjDummy.SynchMeBefore()
        /// </summary>
        /// <param name="t"></param>
        void SynchAfterAction(double t) {
            FillCells();
            FillNeibs();
            UpdateParticles();

          //  var px0 = AllParticles.FirstOrDefault(p => p.X == 0d) as IsotropicGasParticle;
        }

        /// <summary>
        /// Вычисляет индекс ячейки (колонку) в сетке CellNet, подходящей для частицы
        /// </summary>
        /// <param name="particle"></param>
        /// <returns></returns>
        int GetHashXInd(IParticle2D particle) {
            return (int)Floor((particle.X - xmin) / hmax);
        }

        /// <summary>
        /// Вычисляет индекс ячейки (строку) в сетке CellNet, подходящей для частицы
        /// </summary>
        /// <param name="particle"></param>
        /// <returns></returns>
        int GetHashYInd(IParticle2D particle) {
            return (int)Floor((particle.Y - ymin) / hmax);
        }
        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="integrParticles">Интегрируемые частицы</param>
        /// <param name="wall">неинтегрируемые частицы</param>
        public Sph2D(IEnumerable<IParticle2D> integrParticles,IEnumerable<IParticle2D> wall) {
            Particles.AddRange(integrParticles);
            if(wall != null)
                WallParticles.AddRange(wall);
            AllParticles.AddRange(integrParticles.Concat(WallParticles));

            MaxStuffCount = AllParticles.Max(p => p.StuffCount);
            hmax = AllParticles.Max(p => p.GetHmax());

            int i = 0;
            foreach(var p in Particles) {
                p.Name = $"particle{i++}";
                if(p is IScnObj)
                    AddChildUnsafe(p as IScnObj);
            }
            i = 0;
            foreach(var w in WallParticles) {
                w.Name = $"wall{i++}";
            }

            LostPatricleStack.PushRange(AllParticles.ToArray());
            RebuildCells();

            SynchMeAfter += SynchAfterAction;
        }
        #endregion

        #region Static methods

        #endregion
    }
}
