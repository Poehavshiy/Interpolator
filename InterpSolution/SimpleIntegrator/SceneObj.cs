using Interpolator;
using Microsoft.Research.Oslo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SimpleIntegrator {
    public delegate bool FlagFunct(params SolPoint[] sp);

    public interface IScnObj : INamedChild, IDisposable {
        List<string> AllParamsNames { get; }
        IEnumerable<IScnPrm> GetAllParams();
        Vector GetAllParamsValues(double t,Vector y);
        List<IScnObj> Children { get; }
        IScnPrm FindParam(string paramName);
        int AddParam(IScnPrm prm);
        void RemoveParam(IScnPrm prm);
        void AddChild(IScnObj child);
        void RemoveChild(IScnObj child);
        Vector Rebuild(double toTime);
        IEnumerable<IScnPrm> GetDiffPrms();
        void AddDiffPropToParam(IScnPrm prm,IScnPrm dPrmDt,bool removeOldDt,bool getNewName);
        int ResetAllParams();
        void ResetParam(string nameOfProp);
        void SynchMe(double t);
        void RebuildStruct();
        Action<double> SynchMeBefore { get; set; }
        Action<double> SynchMeAfter { get; set; }
        Action<double> SynchMeForNext { get; set; }
        Action RebuildStructureAction { get; set; }
        void SetParam(string name,object value);


        List<ILaw> Laws { get; }
        void AddLaw(ILaw newLaw);
        bool ApplyLaws();

        Vector f(double t,Vector y);
        Vector f_parallel(double t,Vector y);

        Dictionary<string,FlagFunct> FlagDict { get; }
    }

    public class ScnObjDummy : NamedChild, IScnObj {
        public IScnPrm[] DiffArr { get; private set; }
        public int DiffArrN { get; private set; } = -1;
        public List<IScnPrm> Prms { get; set; } = new List<IScnPrm>();
        public List<IScnObj> Children { get; set; } = new List<IScnObj>();
        public List<string> AllParamsNames { get; set; } = new List<string>();
        public List<ILaw> Laws { get; } = new List<ILaw>();
        public Action<double> SynchMeBefore { get; set; } = null;
        public Action<double> SynchMeAfter { get; set; } = null;
        public Action<double> SynchMeForNext { get; set; } = null;
        public Action RebuildStructureAction { get; set; } = null;
        public Dictionary<string,FlagFunct> FlagDict { get; set; } = new Dictionary<string,FlagFunct>();
        


        private ParallelQuery<IScnPrm> DiffArr_parallel;
        public double TimeSynch { get; protected set; }

        public void SynchMeTo(double t,ref Vector y) {
            //if(DiffArrN != y.Length)
            //    throw new InvalidOperationException("разные длины векторов in out");
            for(int i = 0; i < DiffArrN; i++) {
                DiffArr[i].SetVal(y[i]);
            }
            SynchMe(t);
            
        }

        public void SynchMeTo(SolPoint sp) {
            var v = sp.X;
            var t = sp.T;
            SynchMeTo(t,ref v);
        }

        public Vector f(double t,Vector y) {

            SynchMeTo(t,ref y);
            
            var result = Vector.Zeros(DiffArrN);
            for(int i = 0; i < DiffArrN; i++) {
                result[i] = DiffArr[i].MyDiff.GetVal(t);
            }
            SynchMeForNext?.Invoke(t);
            return result;
        }

        public Vector Rebuild(double toTime = 0.0d) {
            TimeSynch = toTime;
            RebuildStruct();
            if(!ApplyLaws())
                throw new Exception("Структура неправильная. Невозможно реализовать все Laws");

            AllParamsNames.Clear();
            foreach(var prm in GetAllParams()) {
                AllParamsNames.Add(prm.FullName);
                prm.NumInVector = -1;
            }

            var diffArrSeq = GetDiffPrms();
            foreach(var child in Children) {
                diffArrSeq = diffArrSeq.Concat(child.GetDiffPrms());
            }
            DiffArr = diffArrSeq.ToArray();
            DiffArrN = DiffArr.Length;
            for(int i = 0; i < DiffArrN; i++) {
                DiffArr[i].NumInVector = i;
            }
            DiffArr_parallel = DiffArr.AsParallel();
            return VectorCurrent;
        }

        public Vector VectorCurrent {
            get {
                return new Vector(DiffArr.Select(prm => prm.GetVal(TimeSynch)).ToArray());
            }
        }

        public bool ApplyLaws() {
            foreach(var law in Laws) {
                if(!law.ApplyMe())
                    return false;
            }

            foreach(var child in Children) {
                if(!child.ApplyLaws())
                    return false;
            }

            return true;
        }

        public IEnumerable<IScnPrm> GetDiffPrms() {
            var chldrenDiffs = Enumerable.Empty<IScnPrm>();
            foreach(var ch in Children) {
                chldrenDiffs = chldrenDiffs.Concat(ch.GetDiffPrms());
            }
            return Prms.Where(a => a.MyDiff != null).Concat(chldrenDiffs);
        }

        public void SynchMe(double t) {

            for(int i = 0; i < Prms.Count; i++) {
                if(Prms[i].IsNeedSynch)
                    Prms[i].SetVal(t);
            }

            SynchMeBefore?.Invoke(t);

            foreach(var child in Children) {
                child.SynchMe(t);
            }

            SynchMeAfter?.Invoke(t);

            TimeSynch = t;
        }

        public int AddParam(IScnPrm prm) {
            if(!Prms.Contains(prm)) {
                if(prm.Owner != null && prm.Owner != this)
                    prm.Owner.RemoveParam(prm);

                Prms.Add(prm);
                prm.Owner = this;
            }
            return Prms.Count;
        }
        public void AddDiffPropToParam(IScnPrm prm,IScnPrm dPrmDt,bool removeOldDt = true,bool getNewName = false) {
            if(!Prms.Contains(prm))
                AddParam(prm);
            if(removeOldDt && prm.MyDiff != null && Prms.Contains(prm.MyDiff))
                Prms.Remove(prm.MyDiff);
            if(getNewName)
                dPrmDt.Name = prm.Name + "'";
            prm.MyDiff = dPrmDt;
            if(dPrmDt.Owner == null)
                AddParam(dPrmDt);
        }
        public void RemoveParam(IScnPrm prm) {
            if(Prms.Contains(prm)) {
                Prms.Remove(prm);
                prm.GetVal = null;
                prm.SetVal = null;
                prm.MyDiff = null;
            }

        }
        public void RemoveParam(String prmName) {
            var pX = FindParam(prmName);
            if(pX != null)
                RemoveParam(pX);
        }
        public IScnPrm FindParam(string paramName) {
            var reg = new Regex("\\b" + paramName,RegexOptions.IgnoreCase);
            return Prms.Where(elem => reg.IsMatch(elem.Name)).FirstOrDefault();
        }

        public void AddChild(IScnObj newChild) {
            IScnObj hasWThisName = null;
            int ind = 0;
            do {
                hasWThisName = Children.FirstOrDefault(ch => ch.Name == newChild.Name);
                newChild.Name =
                    hasWThisName != null ?
                    Regex.Replace(hasWThisName.Name,@"\d+$","") + (++ind).ToString() :
                    newChild.Name;

            } while(hasWThisName != null);

            Children.Add(newChild);
            newChild.Owner = this;
        }

        public void AddChildUnsafe(IScnObj newChild) {
            Children.Add(newChild);
            newChild.Owner = this;
        }

        public void RemoveChild(IScnObj child) {
            if(Children.Remove(child))
                child.Owner = null;
        }

        public Vector GetAllParamsValues(SolPoint sp) {
            return GetAllParamsValues(sp.T,sp.X);
        }
        public Vector GetAllParamsValues(double t,Vector y) {
            SynchMeTo(t,ref y);
            var res = Vector.Zeros(AllParamsNames.Count());
            int i = 0;
            foreach(var prm in GetAllParams()) {
                res[i++] = prm.GetVal(t);
            }
            return res;
        }
        public IEnumerable<IScnPrm> GetAllParams() {
            var res = Prms.Select(prm => prm);
            foreach(var child in Children) {
                res = res.Concat(child.GetAllParams());
            }
            return res;
        }

        public void AddLaw(ILaw newLaw) {
            var alredyHas = Laws.FirstOrDefault(lw => lw.Name == newLaw.Name);

            if(alredyHas != null)
                throw new KeyNotFoundException("Такой закон уже тут есть!");

            Laws.Add(newLaw);
            newLaw.Owner = this;

        }

        public void ResetParam(string nameOfProp) {
            RemoveParam(nameOfProp);
            var currVal = (double)this.GetType().GetProperty(nameOfProp).GetGetMethod().Invoke(this,null);
            
            var scnParam = new ScnPrm(nameOfProp,this,currVal);
            this.GetType().GetProperty("p" + nameOfProp).GetSetMethod().Invoke(this,new object[] { scnParam });

            var GetDeleg = (Func<double>)this.GetType().GetProperty(nameOfProp).GetGetMethod().CreateDelegate(typeof(Func<double>),this);
            var SetDeleg = (Action<double>)this.GetType().GetProperty(nameOfProp).GetSetMethod().CreateDelegate(typeof(Action<double>),this);

            scnParam.GetVal = t => GetDeleg();
            scnParam.SetVal = SetDeleg;
        }

        public int ResetAllParams() {
            var propsNames =
                from prInf in this.GetType().GetProperties()
                join prInfP in this.GetType().GetProperties()
                on "p" + prInf.Name equals prInfP.Name
                select prInf.Name;
            int i = 0;
            foreach(var propname in propsNames) {
                ResetParam(propname);
                ++i;
            }
            return i;


        }

        public void RebuildStruct() {
            RebuildStructureAction?.Invoke();
            foreach(var child in Children) {
                child.RebuildStruct();
            }
        }

        public ScnObjDummy() {
            FlagDict.Add("TimeLimit",TimeFlag);
            ResetAllParams();
        }

        public ScnObjDummy(string name):this() {
            Name = name;
        }

        public void SetParam(string name,object value) {
            var param = this.FindParam(name);
            if(param == null)
                return;
            if(IsNumber(value)) {
                param.SetVal(ToDouble(value));
            } else if(value is InterpXY) {
                param.SealInterp((InterpXY)value);
            } else if(value is Func<double,double>) {
                param.GetVal = value as Func<double,double>;
            }
        }

        public bool SetDiff(string prmName, Func<double,double> dPrmdtF, string dname = "") {
            return SetDiff(FindParam(prmName),dPrmdtF,dname);
        }
        public bool SetDiff(IScnPrm prm,Func<double,double> dPrmdtF,string dname = "") {
            if(!Prms.Contains(prm))
                return false;
            var dprm = new ScnPrm();
            dprm.GetVal = new Func<double,double>(dPrmdtF);

            bool newname = dname == "";
            if(!newname)
                dprm.Name = dname;
            AddDiffPropToParam(prm,dprm,getNewName: newname);
            return true;

        }

        public static bool IsNumber(object value) {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }

        public static double ToDouble(object value) {
            return value is sbyte ? (sbyte)value:
                   value is byte ? (byte)value :
                   value is short ? (short)value :
                   value is ushort ? (ushort)value :
                   value is int ? (int)value :
                   value is uint ? (uint)value :
                   value is long ? (long)value :
                   value is ulong ? (ulong)value :
                   value is float ? (float)value :
                   value is double ? (double)value :
                   value is decimal ? Decimal.ToDouble((decimal)value) :
                   0d;
        }

        public static double TimeLimit { get; set; } = 300d;
        public static bool TimeFlag(params SolPoint[] sp) {
            return sp[0].T >= TimeLimit;
        }

        public virtual void Dispose() {
            DiffArr = null;
            Prms.Clear();
            Prms = null;
            foreach(var ch in Children) {
                ch.Dispose();
            }
            Children.Clear();
            Children = null;
            AllParamsNames.Clear();
            AllParamsNames = null;
            Laws.Clear();
            SynchMeBefore = null;
            SynchMeAfter = null;
            RebuildStructureAction = null;
            FlagDict.Clear();
            FlagDict = null;
        }
        #region Parallel
        public int ThreadsCount { get; set; } = 7;
        private Vector _res;
        private struct StateStruct {
            public double t;
            public int fromind;
            public int toind;
        }
        public Vector f_parallel(double t,Vector y) {
            SynchMeTo(t,ref y);
            _res = Vector.Zeros(DiffArrN);

            int piece = DiffArrN / ThreadsCount;
            var tasks = new List<Task>(DiffArrN);
            for(int i = 0; i < ThreadsCount; i++) {
                StateStruct s;
                s.t = t;
                s.fromind = i * piece;
                s.toind = (i + 1) * piece - 1;
                s.toind = s.toind > DiffArrN ? DiffArrN : s.toind;
                tasks.Add(Task.Factory.StartNew(FillResVector,s));
            }
            Task.WhenAll(tasks);

            return _res;
        }

        private void FillResVector(object state) {
            var str = state ;
            for(int i = ((StateStruct)state).fromind; i <= ((StateStruct)state).toind; i++) {
                _res[i] = DiffArr[i].MyDiff.GetVal(((StateStruct)state).t);
            }
        }
        #endregion
        #region Save/Load
        public IDictionary<string, double> SaveToDict() {
            var res = new Dictionary<string,double>(AllParamsNames.Count+1);
            foreach(var par in GetAllParams()) {
                res.Add(par.FullName,par.GetVal(TimeSynch));
            }
            res.Add("TimeSynch",TimeSynch);
            return res;
        }

        public double LoadFromDict(IDictionary<string,double> dict,bool safeBackup = true) {
            if(dict == null)
                return 0;
            IDictionary<string,double> backup = safeBackup ? SaveToDict() :null;
            try {
                var prms = GetAllParams().ToDictionary(p => p.FullName);
                foreach(var prm in prms) {
                    prm.Value.SetVal(dict[prm.Key]);
                }
                TimeSynch = dict["TimeSynch"];
                return TimeSynch;
            }
            catch(Exception e) {
                return LoadFromDict(backup,false);
                throw e;
            }
            
        }

        #endregion


    }

    public static class DummyIOHelper {

        public static void Serialize(this ScnObjDummy dummy,TextWriter writer) {
            var dict = dummy.SaveToDict();
            Serialize(writer,dict);
        }

        public static void Deserialize(this ScnObjDummy dummy,TextReader reader) {
            var dict = new Dictionary<string,double>();
            Deserialize(reader,dict);
            dummy.LoadFromDict(dict);
        }

        public static void Serialize(TextWriter writer,IDictionary<string,double> dictionary) {
            List<DictEntry> entries = new List<DictEntry>(dictionary.Count);
            foreach(var key in dictionary.Keys) {
                entries.Add(new DictEntry(key,dictionary[key]));
            }
            XmlSerializer serializer = new XmlSerializer(typeof(List<DictEntry>));
            serializer.Serialize(writer,entries);


        }

        public static void Deserialize(TextReader reader,IDictionary dictionary) {
            dictionary.Clear();
            XmlSerializer serializer = new XmlSerializer(typeof(List<DictEntry>));
            List<DictEntry> list = (List<DictEntry>)serializer.Deserialize(reader);
            foreach(DictEntry entry in list) {
                dictionary[entry.Key] = entry.Value;
            }
        }


        public class DictEntry {
            [XmlAttribute]
            public string Key;
            [XmlAttribute]
            public double Value;
            public DictEntry() {
            }

            public DictEntry(string key,double value) {
                Key = key;
                Value = value;
            }
        }
    }


}
