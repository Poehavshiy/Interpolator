using System;
using System.Collections.Generic;
using Sharp3D.Math.Core;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleIntegrator;
using Microsoft.Research.Oslo;

namespace SPH_2D
{
    public class FileReader
    {
        private static List<My_IsotropicGas> ConstructBountPart(double boundaryDensity)
        {
            List<My_IsotropicGas> WallParticles = new List<My_IsotropicGas>();
            foreach (var side in My_Sph2D.boundaries)
            {
                Vector2D vector = side.Item2 - side.Item1;
                int partN = Convert.ToInt32((Math.Abs(vector.GetLength())) * boundaryDensity);
                double xstep = vector.X / partN;
                double ystep = vector.Y / partN;
                for (int i = 0; i < partN; ++i)
                {
                    My_IsotropicGas a = new My_IsotropicGas(1, 1, true);
                    a.X = side.Item1.X + i * xstep;
                    a.Y = side.Item1.Y + i * ystep;
                    WallParticles.Add(a);
                }
            }
            return WallParticles;
        }
        //
        static List<Tuple<Vector2D, Vector2D>> CreateBoundaries(string pathToBoundaries)
        {
            System.IO.StreamReader file =
                 new System.IO.StreamReader(pathToBoundaries);
            List<Tuple<Vector2D, Vector2D>> boundaries_ = new List<Tuple<Vector2D, Vector2D>>();

            string line;
            while ((line = file.ReadLine()) != null)
            {
                string[] numbers = line.Split(',');
                double first = Convert.ToDouble(numbers[0]);
                double second = Convert.ToDouble(numbers[1]);
                Vector2D firstP = new Vector2D(first, second);
                first = Convert.ToDouble(numbers[2]);
                second = Convert.ToDouble(numbers[3]);
                Vector2D secondP = new Vector2D(first, second);
                Tuple<Vector2D, Vector2D> side = new Tuple<Vector2D, Vector2D>(firstP, secondP);
                boundaries_.Add(side);
            }
            return boundaries_;
        }

        public static List<My_IsotropicGas> ConstructBountPart(string pathToBound, double bdensity)
        {
            if (bdensity < 0.01) return null;
            My_Sph2D.boundaries = CreateBoundaries(pathToBound);
            List<My_IsotropicGas> WallParticles = ConstructBountPart(bdensity);
            return WallParticles;
        }

        public static List<My_IsotropicGas> FillRegion(List<Vector2D> region, bool shape, int N)
        {
           
            double e = 4.29 * 10E6;
            double p = 1.63;
            double P = 0.4 * p * e;
            double M = 0;
            double X = Math.Abs(region[0].X - region[3].X);
            double Y = Math.Abs(region[0].Y - region[1].Y);
            if (!shape)
            {
                double r = (region[2].X - region[0].X) / 2;
                M = (p * Math.PI * Math.Pow(r, 2) / 2) / N;
            }
            else
            {
                M = (p * Math.PI * X * Y / 2) / N;
            }
            // double XYratio = X / Y;
            int per_y = (int)Math.Sqrt(N);
            int per_x = per_y;//number / per_y;
            double xstep = X / per_x;
            double ystep = Y / per_y;
            //
            //плотность давление масса скорость (x,y)
            double[] cond = { p, P, M, 0, 0 };
            List<My_IsotropicGas> particles = new List<My_IsotropicGas>();
            double h = 4 * xstep;
            for (int i = 0; i < N; ++i)
            {
                particles.Add(new My_IsotropicGas(1, h, false, cond));
            }

            for (int i = 0; i < per_y; ++i)
            {
                for (int j = 0; j < per_x; ++j)
                {
                    double curX = region[0].X + j * xstep;
                    double curY = region[0].Y + i * ystep;
                    particles[per_x * i + j].X = curX;
                    particles[per_x * i + j].Y = curY;
                }
            }

            if (!shape)
            {
                List<My_IsotropicGas> particlesSphere = new List<My_IsotropicGas>();
                double center_x = (region[2].X + region[0].X) / 2;
                double center_y = (region[2].Y + region[0].Y) / 2;
                double r = (region[2].X - region[0].X) / 2;
                for (int i = 0; i < N; ++i)
                {
                    double curR = Math.Sqrt((center_x - particles[i].X) * (center_x - particles[i].X) +
                        (center_y - particles[i].Y) * (center_y - particles[i].Y));
                    if (curR <= r)
                        particlesSphere.Add(particles[i]);
                }
                return particlesSphere;
            }
            return particles;
        }

        public static List<My_IsotropicGas> ConstructRealPart(string pathToReal)
        {
            //rectangle
            System.IO.StreamReader file =
                 new System.IO.StreamReader(pathToReal);
            string line;
            line = file.ReadLine();
            List<bool> shapes = new List<bool>();//true rect false sphere
            if (line == "rectangle")
            {
                shapes.Add(true);
            }
            else
            {
                shapes.Add(false);
            }

            List<List<Vector2D>> regions = new List<List<Vector2D>>();
            List<Vector2D> region = new List<Vector2D>();
            while ((line = file.ReadLine()) != null)
            {
                if (line != "rectangle" && line != "circle")
                {
                    string[] numbers = line.Split(',');
                    double first = Convert.ToDouble(numbers[0]);
                    double second = Convert.ToDouble(numbers[1]);
                    Vector2D point = new Vector2D(first, second);
                    region.Add(point);
                }
                else
                {
                    regions.Add(region);
                    if (line == "rectangle") shapes.Add(true);
                    else shapes.Add(false);
                    region = new List<Vector2D>();
                }
            }
            regions.Add(region);
            //
            List<My_IsotropicGas> particles = new List<My_IsotropicGas>();

            var regionsAndShapes = regions.Zip(shapes, (n, w) => new { Reg = n, Shape = w });

            foreach (var regShape in regionsAndShapes)
            {
                particles.AddRange(FillRegion(regShape.Reg, regShape.Shape, 10));
            }
            //
            return particles;
        }
    }

    public class My_IsotropicGas : IsotropicGasParticle, IMy_IsotropicGas {
        public bool isboundary { get; set; }

        IMy_IsotropicGas CreateMiracleClone(List<Tuple<Vector2D, Vector2D>> boundaries)
        {
            
            Tuple<double, double> MirrorCoordinates = FindMirrorPartPos(boundaries);
            double[] cond = { Ro, P, M, -Vel.X, -Vel.Y };
            My_IsotropicGasMiracleDummy MiracleParticle = new My_IsotropicGasMiracleDummy(this,false,cond);

            MiracleParticle.X = MirrorCoordinates.Item1;
            MiracleParticle.Y = MirrorCoordinates.Item2;

            return MiracleParticle;
        }

        public My_IsotropicGas(double d, double hmax, bool boundarytype = false, double[] cond = null) : base(d, hmax)
        {
            //cond -  плотность давление масса скорость (x,y)
            isboundary = boundarytype;
            if (cond != null)
            {
                Ro = cond[0];
                P = cond[1];
                M = cond[2];
                Vel.X = cond[3];
                Vel.Y = cond[4];
                E = P / (0.4 * Ro);
            }
        }

        Tuple<double, double, double, double> calcDistanse(Tuple<Vector2D, Vector2D> side)
        {
            double A = side.Item1.Y - side.Item2.Y;
            double B = side.Item2.X - side.Item1.X;
            double C = side.Item1.X * side.Item2.Y - side.Item2.X * side.Item1.Y;
            double distance = Math.Abs((A * X + B * Y + C) / Math.Sqrt(A * A + B * B));
            return new Tuple<double, double, double, double>(A, B, C, distance);
        }
        //принимает границы, находит ближайшую к точке линию и возвращает координаты симетричной точки
        public Tuple<double, double> FindMirrorPartPos(List<Tuple<Vector2D, Vector2D>> boundaries)
        {
            Tuple<double, double, double, double> result = calcDistanse(boundaries[0]);
            foreach (var side in boundaries)
            {
                Tuple<double, double, double, double> current = calcDistanse(side);
                if (current.Item4 < result.Item4)
                {
                    result = current;
                }
            }
            double A = result.Item1;
            double B = result.Item2;
            double C = result.Item3;
            double D = -(A * Y - B * X);
            double Y1 = -(D * A + C * B) / (B * B + A * A);
            //if (Double.IsInfinity(Y1)) Y1 = 0;
            double X1 = (A * Y1 + D) / B;
            //if (Double.IsInfinity(X1)) X1 = 0;
            double X2 = 2 * X1 - X;
            double Y2 = 2 * Y1 - Y;
            return new Tuple<double, double>(X2, Y2);
        }
        //
        public double C()
        {
            return (k * (k - 1) * E);
        }
        //
        public override void FillDts()
        {


            //Список зеркальных частиц
            var miracleList = new List<IMy_IsotropicGas>();



            //Создаем зеркальные частицы
            //if(Neibs.Any(n => (n as My_IsotropicGas).isboundary))
            foreach(var neib in Neibs.Cast<My_IsotropicGas>().Where(n => !n.isboundary).Concat(new[] { this })) {
                //Если у соседа есть в соседях граница, то 
                if(neib.Neibs.Cast<My_IsotropicGas>().Any(nn => nn.isboundary)) {
                    //Отображаем соседа
                    var mir = neib.CreateMiracleClone(My_Sph2D.boundaries);
                    miracleList.Add(mir);
                }
            }

            foreach (var neib in Neibs.Where(n => GetDistTo(n) < hmax).Cast<IMy_IsotropicGas>())//.Concat(miracleList.Where(mp => mp.GetDistTo(this) < hmax)))
            {
                Vector2D deltaV = Vel.Vec2D - neib.Vel.Vec2D;
                Vector2D deltaR = Vec2D - neib.Vec2D;
                if (!neib.isboundary)
                {
                   
                    double mj = neib.M;
                    double r = deltaR.GetLength();
                    //
                    double H = 0;
                    double scalar = deltaV * deltaR;
                    if  ( scalar < 0)
                    {
                        double alpha_ = 0.9;
                        double beta_ = 0.9;
                        double phi_ = 0.1;
                        double phi = hmax * scalar / (Math.Pow(r, 2) + Math.Pow(phi_ * hmax, 2));
                        double Ro_ = (Ro + neib.Ro) / 2;
                        double C_ = ( C() + neib.C() ) / 2;

                        H = (-alpha_ * C_ * phi + beta_ * Math.Pow(phi, 2)) / Ro_;
                       
                    }
                    //
                    double brackets = mj * ( P / (Ro * Ro) + neib.P / (neib.Ro * neib.Ro) + 0 );
                    if (H > 1)
                    {
                        double res = brackets / H;
                        int a = 0;
                    }
                    Vector2D dW = new Vector2D((deltaR.X / r) * dW_func(r, hmax), (deltaR.Y / r) * dW_func(r, hmax));
                    //плотность
                    //Ro += mj * W_func(r, hmax);
                    //скорости
                    dV.X += -brackets * dW.X;
                    dV.Y += -brackets * dW.Y;
                    //энергия
                    dE += 0.5 * brackets * deltaV * dW;
                }
                else
                {
                    double r0 = 2;
                    double rij = deltaR.GetLength();
                    if (r0 / rij <= 1)
                    {
                        double n1 = 12;
                        double n2 = 4;
                        double D = (Vel.Vec2D.GetLength() > neib.Vel.Vec2D.GetLength()) ?
                            Vel.Vec2D.GetLength() * Vel.Vec2D.GetLength() :
                            neib.Vel.Vec2D.GetLength() * neib.Vel.Vec2D.GetLength();
                        D = D * 2;
                        double brackets = -D * (Math.Pow(r0 / rij, n1) - Math.Pow(r0 / rij, n2)) / (rij * rij);
                        double PDijx = brackets * deltaR.X;
                        double PDijy = brackets * deltaR.Y;
                        //
                        dV.X += PDijx / M;
                        dV.Y += PDijy / M;
                    }
                }
            }

        }

        public override void DoStuff(int stuffIndex)
        {

            switch (stuffIndex)
            {
                case 0:
                    {
                    /*
                     ВОТ ТУТ ИЗМЕНЕНИЕ !!!!
                     ДОБАВИЛ
                     if(!isboundary)
                     
                     */
                        if(!isboundary)
                            SetP();
                        break;
                    }
                case 1:
                    {
                        if (!isboundary)
                            FillDts();
                        break;
                    }

                default:
                    break;
            }
        }

        public override void SetP()
        {

            //Ro = Neibs.Cast<IsotropicGasParticle>().Sum(n => {
            //    double h = alpha * (D + n.D) * 0.5;
            //    double w = W_func(GetDistTo(n),h);
            //    return n.M * w;

            //}) + M* W_func(0,1);
            P = (k - 1d) * Ro * E;
            Ro = M* W_func(0, hmax);
            dE = 0d;
            dV.Vec2D = Vector2D.Zero;
            foreach (var neib in Neibs.Cast<My_IsotropicGas>())
            {
                Ro += neib.M * W_func(this.GetDistTo(neib), hmax);
            }
        }
    }



    public class My_Sph2D : Sph2D
    {

        public static List<Tuple<Vector2D, Vector2D>> boundaries;

        public My_Sph2D(string pathToreal, string pathToBound) :
            base(FileReader.ConstructRealPart(pathToreal), FileReader.ConstructBountPart(pathToBound, 4))
        {
            int i = 0;
            i++;
        }

    }



//Для отображения
    internal interface IMy_IsotropicGas  {
        bool isboundary { get; }
        IPosition2D Vel { get; }
        Vector2D Vec2D { get; }
        double X { get; set; }
        double Y { get; set; }
        double GetDistTo(IParticle2D particle);
        double M { get; }
        double Ro { get; }
        double P { get; }
        double C();


    }

    public class VelDummy : IPosition2D {
        public List<string> AllParamsNames {
            get {
                throw new NotImplementedException();
            }
        }

        public List<IScnObj> Children {
            get {
                throw new NotImplementedException();
            }
        }

        public Dictionary<string,FlagFunct> FlagDict {
            get {
                throw new NotImplementedException();
            }
        }

        public string FullName {
            get {
                throw new NotImplementedException();
            }
        }

        public List<ILaw> Laws {
            get {
                throw new NotImplementedException();
            }
        }

        public string Name {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IScnObj Owner {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IScnPrm pX {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public IScnPrm pY {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public Action RebuildStructureAction {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public Action<double> SynchMeAfter {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public Action<double> SynchMeBefore {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public Action<double> SynchMeForNext {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public Vector2D Vec2D { get { return new Vector2D(X,Y); } set { X = value.X; Y = value.Y; } }
        public double X { get; set; }
        public double Y { get; set; }

        public void AddChild(IScnObj child) {
            throw new NotImplementedException();
        }

        public void AddDiffPropToParam(IScnPrm prm,IScnPrm dPrmDt,bool removeOldDt,bool getNewName) {
            throw new NotImplementedException();
        }

        public void AddDiffVect(IPosition1D dXdt,bool getNewName) {
            throw new NotImplementedException();
        }

        public void AddDiffVect(IPosition2D dV2Ddt,bool getNewName) {
            throw new NotImplementedException();
        }

        public void AddLaw(ILaw newLaw) {
            throw new NotImplementedException();
        }

        public int AddParam(IScnPrm prm) {
            throw new NotImplementedException();
        }

        public bool ApplyLaws() {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public Vector f(double t,Vector y) {
            throw new NotImplementedException();
        }

        public IScnPrm FindParam(string paramName) {
            throw new NotImplementedException();
        }

        public Vector f_parallel(double t,Vector y) {
            throw new NotImplementedException();
        }

        public IEnumerable<IScnPrm> GetAllParams() {
            throw new NotImplementedException();
        }

        public Vector GetAllParamsValues(double t,Vector y) {
            throw new NotImplementedException();
        }

        public IEnumerable<IScnPrm> GetDiffPrms() {
            throw new NotImplementedException();
        }

        public Vector Rebuild(double toTime) {
            throw new NotImplementedException();
        }

        public void RebuildStruct() {
            throw new NotImplementedException();
        }

        public void RemoveChild(IScnObj child) {
            throw new NotImplementedException();
        }

        public void RemoveParam(IScnPrm prm) {
            throw new NotImplementedException();
        }

        public int ResetAllParams() {
            throw new NotImplementedException();
        }

        public void ResetParam(string nameOfProp) {
            throw new NotImplementedException();
        }

        public void SetParam(string name,object value) {
            throw new NotImplementedException();
        }

        public void SynchMe(double t) {
            throw new NotImplementedException();
        }
    }

    public class My_IsotropicGasMiracleDummy: IMy_IsotropicGas {
        public My_IsotropicGas parent;
        public IPosition2D Vel { get; set; }
        public Vector2D Vec2D { get { return new Vector2D(X,Y); } set { X = value.X;  Y = value.Y; } }
        public double X { get; set; }
        public double Y { get; set; }
        public bool isboundary { get; set; }
        public double M { get; set; }
        public double Ro { get; set; }
        public double P { get; set; }
        public double E { get; private set; }

        public double C() {
            return parent.C();
        }
        public double GetDistTo(IParticle2D particle) {
            double deltX = X - particle.X;
            double deltY = Y - particle.Y;
            return Math.Sqrt(deltX * deltX + deltY * deltY);
        }
        public My_IsotropicGasMiracleDummy(My_IsotropicGas parent,bool boundarytype = false,double[] cond = null)
        {
            this.parent = parent;
            Vel = new VelDummy();
            //cond -  плотность давление масса скорость (x,y)
            isboundary = boundarytype;
            if(cond != null) {
                Ro = cond[0];
                P = cond[1];
                M = cond[2];
                Vel.X = cond[3];
                Vel.Y = cond[4];
                E = P / (0.4 * Ro);
            }
        }
    }

    
}

