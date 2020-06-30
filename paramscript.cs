using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;


using System.IO;
using System.Text;

using Newtonsoft.Json;


/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { __out.Add(text); }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { __out.Add(string.Format(format, args)); }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj)); }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj, method_name)); }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private IGH_Component Component; 
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments, 
  /// Output parameters as ref arguments. You don't have to assign output parameters, 
  /// they will have a default value.
  /// </summary>
  private void RunScript(bool reset, string controlComponentName, List<int> dataIn, ref object A)
  {
        if(reset)
    {
      _controlComponentName = controlComponentName;


      string outputpath = @"C:\Users\ddxgo\Documents\Rhino\20200626c#expts\testjsonout.txt";
      string jsonstring;
      ParamsData paramsdata = new ParamsData();
      jsonstring = JsonConvert.SerializeObject(paramsdata);
      File.WriteAllText(outputpath, jsonstring);

      
      GrasshopperDocument.ScheduleSolution(5, SolutionCallback);
    }
  }

  // <Custom additional code> 
  
  private string _controlComponentName;
  private List<int> _dataIn = new List<int>();
  private int _n;

  
  
  public class ParamsData
  {
    private int _NumSliders;
    private List<int> _SliderVals = new List<int>();
    private int _NumPoints;
    private List<Rhino.Geometry.Point3d> _Points = new List<Rhino.Geometry.Point3d>();

    public ParamsData(int NumSliders, List<int> SliderVals, int NumPoints, List<Rhino.Geometry.Point3d> Points)
    {
      _NumSliders = NumSliders;
      _SliderVals = SliderVals;
      _NumPoints = NumPoints;
      _Points = Points;
    }

    
    
    public ParamsData()
    {
      _NumSliders = 5;
      _NumPoints = 5;
      _SliderVals.Add(22);
      _Points.Add(new Rhino.Geometry.Point3d(22, 22, 22));
      

      
      
      _SliderVals.Clear();
      for (int sliderinit = 0; sliderinit < _NumSliders; sliderinit++) {
        _SliderVals.Add((sliderinit - 3) * 11);
      }
   
   
   //   Rhino.Geometry.Point3d testPoint = new Rhino.Geometry.Point3d(0, 0, 0);
      _Points.Clear();
   
      for (int pointsinit = 0; pointsinit < _NumPoints; pointsinit++) {
        _Points.Add(new Rhino.Geometry.Point3d(pointsinit * 22, pointsinit * 22, pointsinit * 22));
      }
   
      
    }
    
  
    public int NumSliders { get {return _NumSliders;} set {_NumSliders = value;} }
    public List<int> SliderVals { get {return _SliderVals;} set {_SliderVals = value;} }
    public int NumPoints { get {return _NumPoints;} set {_NumPoints = value;} }
    public List<Rhino.Geometry.Point3d> Points { get {return _Points;} set {_Points = value;} }
  }

  
  
  private void SolutionCallback(GH_Document doc)
  {

    string[] lines;
    string inputpath = @"C:\Users\ddxgo\Documents\Rhino\20200626c#expts\20200629params1.txt";
    if (File.Exists(inputpath))
    {
      lines = File.ReadAllLines(inputpath);
      _dataIn.Clear();
      foreach(string line in lines)
      {
        _dataIn.Add(Int32.Parse(line));
      }
    }
    else
    {
      Print("file not found");
    }

    _n = _dataIn[0];
    _dataIn.RemoveAt(0);


    Random rnd = new Random();

    List<IGH_DocumentObject> deletions = new List<IGH_DocumentObject>();

    List<IGH_Param> receivingParams = new List<IGH_Param>();

    foreach(IGH_DocumentObject obj in GrasshopperDocument.Objects)
    {
      if (obj.NickName == _controlComponentName)
      {
        deletions.Add(obj);
        IGH_Param tempParam = obj as IGH_Param;
        if (tempParam.SourceCount > 0)
        {
          deletions.AddRange(tempParam.Sources);
        }
        foreach(IGH_Param recip in tempParam.Recipients)
        {
          receivingParams.Add(recip);
        }
      }
    }

    foreach(IGH_DocumentObject delobj in deletions)
    {
      GrasshopperDocument.RemoveObject(delobj, false);
    }

    Grasshopper.Kernel.Parameters.Param_Integer targetParam = new Grasshopper.Kernel.Parameters.Param_Integer();
    targetParam.NickName = _controlComponentName;
    GrasshopperDocument.AddObject(targetParam, false);
    targetParam.Attributes.Pivot = new System.Drawing.PointF(Component.Attributes.Pivot.X + 10, Component.Attributes.Pivot.Y + 70);

    foreach(IGH_Param receivingParam in receivingParams)
    {
      receivingParam.AddSource(targetParam);
    }

    for(int i = 0;i < _n;i++)
    {
      //instantiate  new slider
      Grasshopper.Kernel.Special.GH_NumberSlider slid = new Grasshopper.Kernel.Special.GH_NumberSlider();
      slid.CreateAttributes(); //sets up default values, and makes sure your slider doesn't crash rhino

      //customise slider (position, ranges etc)
      //targetParam.Attributes.Bounds


      int inputcount = targetParam.SourceCount;
      slid.Attributes.Pivot = new System.Drawing.PointF((float) targetParam.Attributes.Pivot.X - slid.Attributes.Bounds.Width - 50, (float) targetParam.Attributes.Pivot.Y + inputcount * 30);
      slid.Slider.Maximum = 50;
      slid.Slider.Minimum = -50;
      slid.Slider.DecimalPlaces = 0;
      // slid.SetSliderValue((decimal) (rnd.Next(-50, 51)));

      if (i + 1 > _dataIn.Count) {
        slid.SetSliderValue(_dataIn[_dataIn.Count - 1]);
      }
      else {
        slid.SetSliderValue(_dataIn[i]);
      }

      //Until now, the slider is a hypothetical object.
      // This command makes it 'real' and adds it to the canvas.
      GrasshopperDocument.AddObject(slid, false);

      //Connect the new slider to this component
      targetParam.AddSource(slid);
    }




  }
  // </Custom additional code> 

  private List<string> __err = new List<string>(); //Do not modify this list directly.
  private List<string> __out = new List<string>(); //Do not modify this list directly.
  private RhinoDoc doc = RhinoDoc.ActiveDoc;       //Legacy field.
  private IGH_ActiveObject owner;                  //Legacy field.
  private int runCount;                            //Legacy field.
  
  public override void InvokeRunScript(IGH_Component owner, object rhinoDocument, int iteration, List<object> inputs, IGH_DataAccess DA)
  {
    //Prepare for a new run...
    //1. Reset lists
    this.__out.Clear();
    this.__err.Clear();

    this.Component = owner;
    this.Iteration = iteration;
    this.GrasshopperDocument = owner.OnPingDocument();
    this.RhinoDocument = rhinoDocument as Rhino.RhinoDoc;

    this.owner = this.Component;
    this.runCount = this.Iteration;
    this. doc = this.RhinoDocument;

    //2. Assign input parameters
        bool reset = default(bool);
    if (inputs[0] != null)
    {
      reset = (bool)(inputs[0]);
    }

    string controlComponentName = default(string);
    if (inputs[1] != null)
    {
      controlComponentName = (string)(inputs[1]);
    }

    List<int> dataIn = null;
    if (inputs[2] != null)
    {
      dataIn = GH_DirtyCaster.CastToList<int>(inputs[2]);
    }


    //3. Declare output parameters
      object A = null;


    //4. Invoke RunScript
    RunScript(reset, controlComponentName, dataIn, ref A);
      
    try
    {
      //5. Assign output parameters to component...
            if (A != null)
      {
        if (GH_Format.TreatAsCollection(A))
        {
          IEnumerable __enum_A = (IEnumerable)(A);
          DA.SetDataList(1, __enum_A);
        }
        else
        {
          if (A is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(A));
          }
          else
          {
            //assign direct
            DA.SetData(1, A);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
      }

    }
    catch (Exception ex)
    {
      this.__err.Add(string.Format("Script exception: {0}", ex.Message));
    }
    finally
    {
      //Add errors and messages... 
      if (owner.Params.Output.Count > 0)
      {
        if (owner.Params.Output[0] is Grasshopper.Kernel.Parameters.Param_String)
        {
          List<string> __errors_plus_messages = new List<string>();
          if (this.__err != null) { __errors_plus_messages.AddRange(this.__err); }
          if (this.__out != null) { __errors_plus_messages.AddRange(this.__out); }
          if (__errors_plus_messages.Count > 0) 
            DA.SetDataList(0, __errors_plus_messages);
        }
      }
    }
  }
}