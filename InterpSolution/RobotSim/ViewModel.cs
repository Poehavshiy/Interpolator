using Microsoft.Research.Oslo;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveODE;
using Sharp3D.Math.Core;
using System.Collections.Generic;

namespace RobotSim {
    public class ViewModel {
        private LineSeries bSer;
        private LineSeries Wheels;

        public VMPropRx<PlotModel,SolPoint> Model1Rx { get; private set; }
        public VMPropRx<List<SolPoint>,SolPoint> SolPointList { get; private set; }
        RobotDynamics _curr4Draw;


        public ViewModel() {
            _curr4Draw = new RobotDynamics();
            _curr4Draw.Rebuild();
            Model1Rx = new VMPropRx<PlotModel,SolPoint>(() => {
                var Model1 = GetNewModel("params","X","Y");
                bSer = new LineSeries() {
                    Title = "Body",
                   // StrokeThickness = 2,
                    Color = OxyColors.Green
                };
                Model1.Series.Add(bSer);

                return Model1;

            },
            (sp,pm) => {
                _curr4Draw.SynchMeTo(sp);
                Draw(sp.T,pm);
                return pm;
            }
            );

            SolPointList = new VMPropRx<List<SolPoint>,SolPoint>(
                () => new List<SolPoint>(),
                (sp,lst) => {
                    lst.Add(sp);
                    return lst;
                });


        }



        public void Draw(double t,PlotModel pm) {
            bSer.Points.Clear();
            _curr4Draw.SynchMe(t);
            var xAxisNorm = -Vector3D.ZAxis;
            var yAxisNorm = Vector3D.YAxis;

            DrawLineBody(0,1,xAxisNorm,yAxisNorm);
            DrawLineBody(1,2,xAxisNorm,yAxisNorm);
            DrawLineBody(2,3,xAxisNorm,yAxisNorm);
            DrawLineBody(3,0,xAxisNorm,yAxisNorm);
            DrawLineBody(4,5,xAxisNorm,yAxisNorm);
            DrawLineBody(5,6,xAxisNorm,yAxisNorm);
            DrawLineBody(6,7,xAxisNorm,yAxisNorm);
            DrawLineBody(7,4,xAxisNorm,yAxisNorm);
            DrawLineBody(0,4,xAxisNorm,yAxisNorm);
            DrawLineBody(1,5,xAxisNorm,yAxisNorm);
            DrawLineBody(2,6,xAxisNorm,yAxisNorm);
            DrawLineBody(3,7,xAxisNorm,yAxisNorm);


            pm.Title = $"{t:0.###} s";
            pm.InvalidatePlot(true);
            //Thread.Sleep(1000);
        }

        void DrawLineBody(int i0, int i1 , Vector3D xAxisNorm, Vector3D yAxisNorm) {
            bSer.Points.Add(new DataPoint(double.NaN,double.NaN));
            bSer.Points.Add(new DataPoint(_curr4Draw.GetUgol(i0) * xAxisNorm,_curr4Draw.GetUgol(i0) * yAxisNorm));
            bSer.Points.Add(new DataPoint(_curr4Draw.GetUgol(i1) * xAxisNorm,_curr4Draw.GetUgol(i1) * yAxisNorm));
        }

        public PlotModel GetNewModel(string title = "",string xname = "",string yname = "") {

            var m = new PlotModel { Title = title };
            m.PlotType = PlotType.Cartesian;
            var linearAxis1 = new LinearAxis();
            linearAxis1.MajorGridlineStyle = LineStyle.Solid;
            linearAxis1.MaximumPadding = 0;
            linearAxis1.MinimumPadding = 0;
            linearAxis1.MinorGridlineStyle = LineStyle.Dot;
            linearAxis1.Position = AxisPosition.Bottom;
            linearAxis1.Title = xname;
            m.Axes.Add(linearAxis1);
            var linearAxis2 = new LinearAxis();
            linearAxis2.MajorGridlineStyle = LineStyle.Solid;
            linearAxis2.MaximumPadding = 0;
            linearAxis2.MinimumPadding = 0;
            linearAxis2.MinorGridlineStyle = LineStyle.Dot;
            linearAxis2.Title = yname;
            m.Axes.Add(linearAxis2);
            return m;
        }
    }
}