using Sharp3D.Math.Core;
using SimpleIntegrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotSim {
    public class RobotDynamics: ScnObjDummy {

        public MaterialObjectNewton Body { get; set; } = new MaterialObjectNewton();
        public double l, h, w;
        public List<RbWheel> wheels = new List<RbWheel>(4);

        public RbSurfFloor floor;
        public ForceCenter FloorForce;



        //public List<RobotWheel> Wheels { get; set; } = new List<RobotWheel>();
        const string DEFNAME = "Enviroment";
        public RobotDynamics(string name = DEFNAME) :base(name) {
            l = 0.2; //z
            h = 0.05;//y
            w = 0.1;//x
            Body.Mass.Value = 0.5;
            Body.Name = "Body";
            AddChild(Body);
            Body.SynchMeAfter += SynchWheelsToBodyPos;

            FloorForce = new ForceCenter(1,new Position3D(0,1,0),null);
            Body.AddForce(FloorForce);
            SynchMassGeometry();

            for(int i = 0; i < 4; i++) {
                wheels.Add(RbWheel.GetStandart());
            }
            wheels[0].localPos = new Vector3D(w / 2,0,l / 2);
            wheels[1].localPos = new Vector3D(w / 2,0,-l / 2);
            wheels[2].localPos = new Vector3D(-w / 2,0,l / 2);
            wheels[3].localPos = new Vector3D(-w / 2,0,-l / 2);
            //double moment = 0.1;
            //foreach(var w in wheels) {
            //    Body.AddChild(w);
            //    w.Mx = moment;
            //    moment += moment;
            //}
            
        }


        public void SynchMassGeometry() {
            Body.Mass.Ix = Body.Mass.Value * (h * h + l * l) / 12d;
            Body.Mass.Iy = Body.Mass.Value * (w * w + l * l) / 12d;
            Body.Mass.Iz = Body.Mass.Value * (h * h + w * w) / 12d;
        }

        public void SynchWheelsToBodyPos(double t = 0) {
            var matr = Body.WorldTransform;
            foreach(var wheel in wheels) {
                wheel.Vec3D = matr * wheel.localPos;
            }


            
        }

        public Vector3D GetUgol(int ugolInd) {
            switch(ugolInd) {
                case 0: {
                    return Body.WorldTransform*(new Vector3D(w / 2,-h / 2,l / 2));
                }
                case 1: {
                    return Body.WorldTransform * (new Vector3D(w / 2,-h / 2,-l / 2));
                }
                case 2: {
                    return Body.WorldTransform * (new Vector3D(-w / 2,-h / 2,-l / 2));
                }
                case 3: {
                    return Body.WorldTransform * (new Vector3D(-w / 2,-h / 2,l / 2));
                }
                case 4: {
                    return Body.WorldTransform * (new Vector3D(w / 2,h / 2,l / 2));
                }
                case 5: {
                    return Body.WorldTransform * (new Vector3D(w / 2,h / 2,-l / 2));
                }
                case 6: {
                    return Body.WorldTransform * (new Vector3D(-w / 2,h / 2,-l / 2));
                }
                case 7: {
                    return Body.WorldTransform * (new Vector3D(-w / 2,h / 2,l / 2));
                }
                default:
                return Body.WorldTransform * (Vector3D.Zero);
              
            }
        }

    }

}
