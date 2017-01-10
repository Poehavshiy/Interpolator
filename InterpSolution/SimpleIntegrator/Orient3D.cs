using System;
using Sharp3D.Math.Core;
using System.Collections.Generic;
using System.Linq;

namespace SimpleIntegrator {


    public interface IOrient3D:IScnObj {
        /// <summary>
        /// матрица Перехода из Локальнойй СК в глобальную СК 
        /// </summary>
        Matrix4D M { get; }
        /// <summary>
        /// матрица перехода из Глобальной СК в локальную СК
        /// </summary>
        Matrix4D M_1 { get; }
        /// <summary>
        /// матрица вращения из Локальнойй СК в глобальнуюСК 
        /// </summary>
        Matrix3D MRot { get; }
        /// <summary>
        /// матрица вращения из Глобальной СК в локальную СК
        /// </summary>
        Matrix3D MRot_1 { get; }
        /// <summary>
        /// матрица перехода из Локальнойй СК в МИРОВУЮ СК
        /// </summary>
        Matrix4D WorldTransform { get; }
        /// <summary>
        /// матрица перехода из МИРОВОЙ СК в локальную СК
        /// </summary>
        Matrix4D WorldTransform_1 { get; }
        /// <summary>
        /// матрица вращения из Локальнойй СК в МИРОВУЮ СК
        /// </summary>
        Matrix3D WorldTransformRot { get; }
        /// <summary>
        /// матрица вращения из МИРОВОЙ СК в локальную СК
        /// </summary>
        Matrix3D WorldTransformRot_1 { get; }

        List<IOrient3D> TransformChain { get; }
        QuaternionD Q { get; set; }

        double Qw { get; set; }
        double Qx { get; set; }
        double Qy { get; set; }
        double Qz { get; set; }
        
        IScnPrm pQw { get; set; }
        IScnPrm pQx { get; set; }
        IScnPrm pQy { get; set; }
        IScnPrm pQz { get; set; }
    }

 

    public class Orient3D : Position3D, IOrient3D {
        public List<IOrient3D> TransformChain { get; private set; } = new List<IOrient3D>();
        private Matrix4D m = Matrix4D.Identity;
        private Matrix4D m_1 = Matrix4D.Identity;
        private Matrix4D worldTransform = Matrix4D.Identity;
        private Matrix4D worldTransform_1 = Matrix4D.Identity;
        private QuaternionD q = QuaternionD.Identity;

        public Matrix3D MRot { get { return m.Rot; } }
        public Matrix3D MRot_1 { get { return m_1.Rot; } }
        public QuaternionD Q {
            get { return q; }
            set {
                q = value;
                SynchQandM();
            }
        }
        public double Qw {
            get {
                return q.W;
            }
            set {
                q.W = value;
            }
        }
        public double Qx {
            get {
                return q.X;
            }
            set {
                q.X = value;
            }
        }
        public double Qy {
            get {
                return q.Y;
            }
            set {
                q.Y = value;
            }
        }
        public double Qz {
            get {
                return q.Z;
            }
            set {
                q.Z = value;
            }
        }
        public IScnPrm pQw { get; set; }
        public IScnPrm pQx { get; set; }
        public IScnPrm pQy { get; set; }
        public IScnPrm pQz { get; set; }

        public Matrix3D WorldTransformRot {
            get {
                return worldTransform.Rot;
            }
        }

        public Matrix3D WorldTransformRot_1 {
            get {
                return worldTransform_1.Rot;
            }
        }

        public Matrix4D M {
            get {
                return m;
            }
        }

        public Matrix4D M_1 {
            get {
                return m_1;
            }
        }

        public Matrix4D WorldTransform {
            get {
                return worldTransform;
            }
        }

        public Matrix4D WorldTransform_1 {
            get {
                return worldTransform_1;
            }
        }

        public Vector3D XAxis {
            get {
                return WorldTransformRot * Vector3D.XAxis;
            }
        }

        public Vector3D YAxis {
            get {
                return WorldTransformRot * Vector3D.YAxis;
            }
        }

        public Vector3D ZAxis {
            get {
                return WorldTransformRot * Vector3D.ZAxis;
            }
        }

        public void SynchQandM() {
            q.Normalize();
            m = QuaternionD.QuaternionToMatrix(q);
            m.Col4 = Vec3D;
            m_1 = Matrix4D.Inverse(m);

            if(TransformChain.Count == 0) {
                worldTransform = m;
                worldTransform_1 = m_1;
                return;
            }


            worldTransform = M;

            foreach(var orient in TransformChain) {
                worldTransform = orient.M * worldTransform;
            }
            worldTransform_1 = Matrix4D.Inverse(worldTransform);
        }

        protected void MySynchMeBefore(double t) {
            SynchQandM();
        }

        const string DEFNAME = "Orient3D";

        public Orient3D(object x,object y,object z,string name = DEFNAME) :base(x,y,z,name) {
            SynchMeBefore += MySynchMeBefore;
            RebuildStructureAction += RebuildTransformChain;
        }

        public Orient3D(Vector3D posVec,string name = DEFNAME) : this(posVec.X,posVec.Y,posVec.Z,name) { }
        public Orient3D(string name = DEFNAME) : this(0d,0d,0d,name) { }


        public void RebuildTransformChain() {
            TransformChain.Clear();
            var daddy = Owner;
            while(daddy != null) {
                if(daddy is IOrient3D) {
                    TransformChain.Add(daddy as IOrient3D);
                    continue;
                }
                var orient = (IOrient3D)daddy.Children.FirstOrDefault(ch => ch is IOrient3D);
                if(this.Equals(orient))
                    return;
                if(orient != null)
                    TransformChain.Add(orient);

            }
        }
        public void RotateVecToVec(Vector3D oldVec, Vector3D toVec) {
            var rot = QuaternionD.FromTwoVectors(oldVec,toVec);
            q = rot*q;
            SynchQandM();
        }

        public void RotateOXtoVec(Vector3D toVec) {
            RotateVecToVec(WorldTransformRot * Vector3D.XAxis,toVec);
        }
        public void RotateOYtoVec(Vector3D toVec) {
            RotateVecToVec(WorldTransformRot * Vector3D.YAxis,toVec);
        }
        public void RotateOZtoVec(Vector3D toVec) {
            RotateVecToVec(WorldTransformRot * Vector3D.ZAxis,toVec);
        }

    }

    //public class Orient2D : ScnObjDummy, IOrient2D {
    //    private Matrix2D m = Matrix2D.Identity;
    //    private Matrix2D m_1 = Matrix2D.Identity;
    //    private double angleX = 0d;

    //    public Vector2D Loc2Glob(Vector2D loc) {
    //        return m * loc;
    //    }

    //    public Vector2D Glob2Loc(Vector2D wrld) {
    //        return m_1 * wrld;
    //    }

    //    public Matrix2D M { get { return m; } }
    //    public Matrix2D M_1 { get { return m_1; } }
    //    /// <summary>
    //    /// от -PI до PI в итоге)
    //    /// </summary>
    //    public double AngleX {
    //        get { return angleX; }
    //        set {
    //            angleX = value % MathFunctions.PI;
    //            SynchAngleAndM();
    //        }
    //    }

    //    public IScnPrm pAngleX { get; set; }

    //    private void SynchAngleAndM() {
    //        var sin = System.Math.Sin(angleX);
    //        var cos = System.Math.Cos(angleX);
    //        m.M11 = cos;
    //        m.M12 = -sin;
    //        m.M21 = sin;
    //        m.M22 = cos;

    //        m.M11 = cos;
    //        m.M12 = sin;
    //        m.M21 = -sin;
    //        m.M22 = cos;
    //    }

    //    protected void MySynchMeBefore(double t) {
    //        SynchAngleAndM();
    //    }

    //    public Orient2D() {
    //        SynchMeBefore = new System.Action<double>(MySynchMeBefore);
    //        //ResetAllParams();
    //    }

    //}
}
