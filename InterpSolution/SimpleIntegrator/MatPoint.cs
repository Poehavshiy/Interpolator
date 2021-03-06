﻿using Sharp3D.Math.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleIntegrator {
    public interface IVelPos3D: IPosition3D {
        IPosition3D Vel { get; set; }
    }

    public interface IMaterialPoint : IOrient3D {
        IPosition3D Vel { get; set; }
        IPosition3D Acc { get; set; }
        IMassPoint Mass { get; set; }
        List<IForceCenter> Forces { get; set; }
        void AddForce(IForceCenter force,bool createNewSK);
        Vector3D GetImpulse();
        double GetEnergy();

    }
    /// <summary>
    /// X, y, z, - желательно, чтобы были в WORLD СК
    /// </summary>
    public class MaterialPointNewton : Orient3D, IMaterialPoint, IVelPos3D {
        public IPosition3D Vel { get; set; }
        public IPosition3D Acc { get; set; }
        public IMassPoint Mass { get; set; }
        public List<IForceCenter> Forces { get; set; }
        const string DEFNAME = "MatPoint";
        public MaterialPointNewton(object x,object y,object z,string name = DEFNAME):base(x,y,z,name) {
            Vel = new Position3D("Vel");
            Acc = new Position3D("Acc");
            Mass = Mass == null ? new MassPoint(): Mass;
            Forces = new List<IForceCenter>();
            AddChild(Vel);
            AddChild(Acc);
            AddChild(Mass);

            AddDiffVect(Vel,false);
            Vel.AddDiffVect(Acc,false);
            SynchMeAfter += NewtonLaw;
        }

        public MaterialPointNewton(Vector3D posVec,string name = DEFNAME) : this(posVec.X,posVec.Y,posVec.Z,name) { }
        public MaterialPointNewton(string name = DEFNAME) : this(0d,0d,0d,name) { }

        public virtual void NewtonLaw(double t) {
            Vector3D fsumm = Vector3D.Zero;
            foreach(var force in Forces) {
                fsumm += force.VecWorld;
            }
            Acc.Vec3D = fsumm / Mass.Value;
        }

        public virtual void AddForce(IForceCenter force,bool createNewSK = false) {
            AddChild(force as IScnObj);
            Forces.Add(force);
            force.SK = createNewSK || force.SK == null ? this : force.SK;
        }

        public virtual Vector3D GetImpulse() {
            return Mass.Value * Vel.Vec3D;
        }

        public virtual double GetEnergy() {
            var vel =  Vel.Vec3D.GetLength();
            return Mass.Value * vel * vel;
        }
    }

    /// <summary>
    /// Угловые ускорения и скорости беруться в СВЯЗАННОЙ СК
    /// </summary>
    public interface IMaterialObject : IMaterialPoint {
        List<IForceCenter> Moments { get; set; }
        List<IForce> ForcesWithFPoints { get; set; }
        IPosition3D Omega { get; set; }
        IPosition3D Eps { get; set; }
        new IMass3D Mass { get; set; }
        void AddMoment(IForceCenter moment,bool createNewSK);
        Vector3D GetL();
        double GetRotEnergy();
    }

    public class MaterialObjectNewton : MaterialPointNewton, IMaterialObject, IVelPos3D {
        public new IMass3D Mass { get; set; } = new Mass3D();
        public IPosition3D Omega { get; set; } = new Position3D("Omega");
        public IPosition3D Eps { get; set; } = new Position3D("Eps");
        public List<IForceCenter> Moments { get; set; }= new List<IForceCenter>();
        public List<IForce> ForcesWithFPoints { get; set; } = new List<IForce>();
        const string DEFNAME = "MatObj";
        public MaterialObjectNewton(object x,object y,object z,string name = DEFNAME) : base(x,y,z,name) {
            AddChild(Omega);
            AddChild(Eps);

            Omega.AddDiffVect(Eps,false);
            AddDiffPropToParam(pQw,pdQWdt);
            AddDiffPropToParam(pQx,pdQXdt);
            AddDiffPropToParam(pQy,pdQYdt);
            AddDiffPropToParam(pQz,pdQZdt);
        }

        public MaterialObjectNewton(Vector3D posVec,string name = DEFNAME) : this(posVec.X,posVec.Y,posVec.Z,name) { }
        public MaterialObjectNewton(string name = DEFNAME) : this(0d,0d,0d,name) { }

        public override void NewtonLaw(double t) {
            base.NewtonLaw(t);
            var momSum = Vector3D.Zero;
            foreach(var mom in Moments) {
                momSum += mom.VecWorld;
            }
            foreach(var force in ForcesWithFPoints) {
                momSum += force.MomentWorld;
            }
            momSum = WorldTransformRot * momSum;
            var om = Omega.Vec3D;
            Eps.Vec3D = Mass.Tensor.Inverse* (momSum - Vector3D.CrossProduct(om,Mass.Tensor * om));
            dQWdt =-0.5 * (om.X * Qx + om.Y * Qy + om.Z * Qz);
            dQXdt = 0.5 * (om.X * Qw - om.Y * Qz + om.Z * Qy);
            dQYdt = 0.5 * (om.Y * Qw - om.Z * Qx + om.X * Qz);
            dQZdt = 0.5 * (om.Z * Qw - om.X * Qy + om.Y * Qx);
        }
        public override void AddForce(IForceCenter force,bool createNewSK = false) {
            base.AddForce(force,createNewSK);
            if(force is IForce)
                ForcesWithFPoints.Add(force as IForce);
        }
        public virtual void AddMoment(IForceCenter moment,bool createNewSK = false) {
            AddChild(moment as IScnObj);
            Moments.Add(moment);
            moment.SK = createNewSK ? this : moment.SK;
        }
        public IScnPrm pdQWdt { get; set; }
        public IScnPrm pdQXdt { get; set; }
        public IScnPrm pdQYdt { get; set; }
        public IScnPrm pdQZdt { get; set; }
        public double dQWdt { get; set; }
        public double dQXdt { get; set; }
        public double dQYdt { get; set; }
        public double dQZdt { get; set; }

        public override double GetEnergy() {
            return GetRotEnergy() * base.GetEnergy();
        }

        public Vector3D GetL() {
            return Mass.Tensor * Omega.Vec3D;
        }

        public double GetRotEnergy() {
            var om = Omega.Vec3D;
            return 0.5*om * (Mass.Tensor * om);
        }
    }
}
