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
  private void RunScript(bool reset, string path, string controlComponentName, ref object A)
  {
    
    if(reset)
    {
      _controlComponentName = controlComponentName;
      _path = path;
      GrasshopperDocument.ScheduleSolution(5, SolutionCallback);
    }
  }

  // <Custom additional code> 
  
  private string _path;
  private string _controlComponentName;
  private List<int> _dataIn = new List<int>();
  private List<int> _n = new List<int>();
  private List<Rhino.Geometry.Point3d> _pointsdata = new List<Rhino.Geometry.Point3d>();

  public class ParamsData //data for storage in text file
  {
    private List<int> _NumSliders = new List<int>(); //creates a set of slider banks; list NumSliders is sliders per bank
    private List<int> _SliderVals = new List<int>(); //all sliders in all banks in one big list
    private int _NumPoints;                          //number of geometry points
    private List<Rhino.Geometry.Point3d> _Points = new List<Rhino.Geometry.Point3d>();  //geometry points

    public ParamsData(List<int> NumSliders, List<int> SliderVals, int NumPoints, List<Rhino.Geometry.Point3d> Points)
    {
      _NumSliders = NumSliders;
      _SliderVals = SliderVals;
      _NumPoints = NumPoints;
      _Points = Points;
    }

    public ParamsData()
    {
      _NumSliders.Clear();
      _NumPoints = 0;
      _SliderVals.Clear();
      _Points.Clear();
    }

    public List<int> NumSliders { get {return _NumSliders;} set {_NumSliders = value;} }
    public List<int> SliderVals { get {return _SliderVals;} set {_SliderVals = value;} }
    public int NumPoints { get {return _NumPoints;} set {_NumPoints = value;} }
    public List<Rhino.Geometry.Point3d> Points { get {return _Points;} set {_Points = value;} }
  }


  public class OutputParam //class for keeping track of the output connections for the slider grouping params
  {
    private int _outParam;
    private List<IGH_Param> _recvParams;

    public OutputParam()
    {
      _outParam = 0;
      _recvParams = new List<IGH_Param>();
    }

    public OutputParam(int outParam, List<IGH_Param> recvParams)
    {
      _outParam = outParam;
      _recvParams = recvParams;
    }

    public int outParam { get {return _outParam;} set {_outParam = value;}}
    public List<IGH_Param> recvParams { get {return _recvParams;} set {_recvParams = value;}}
  }


  private void SolutionCallback(GH_Document doc)
  {
    //read file, deserialize json, send variables out of the extracted class

    string jsonstring = File.ReadAllText(_path);
    ParamsData paramdata = new ParamsData();
    paramdata = JsonConvert.DeserializeObject<ParamsData>(jsonstring);
    _n = paramdata.NumSliders;
    _dataIn = paramdata.SliderVals;
    _pointsdata = paramdata.Points;


    Random rnd = new Random();

    List<IGH_DocumentObject> deletions = new List<IGH_DocumentObject>();  //list of objects to delete from grasshopper document
    List<OutputParam> outputParams = new List<OutputParam>();  //list of the slider grouping params and their output connections
    List<IGH_Param> PointRecvParams = new List<IGH_Param>();  //list of what the point param is connected to
    
    foreach(IGH_DocumentObject obj in GrasshopperDocument.Objects)
    {
      if (obj.NickName.StartsWith(_controlComponentName)) //the point and integer params i've created
      {
        deletions.Add(obj);
        IGH_Param tempParam = obj as IGH_Param;  //cast obj into a param to locate sources and recipients
        if (tempParam.SourceCount > 0)
        {
          deletions.AddRange(tempParam.Sources);  //add source sliders to deletions list
        }

        if (obj.NickName.StartsWith(_controlComponentName + "points"))
        {
          foreach(IGH_Param recip in tempParam.Recipients)
          {
            PointRecvParams.Add(recip);
          }
        }
        
        if (obj.NickName.StartsWith(_controlComponentName + "slids"))  //the integer params
        {
          int ObjectIndex;
          Int32.TryParse(System.Text.RegularExpressions.Regex.Match(obj.NickName, @"(\d+)\z").Value, out ObjectIndex);  //regex to extract index number from end of param name
          List<IGH_Param> receivingParams = new List<IGH_Param>();
          foreach(IGH_Param recip in tempParam.Recipients)
          {
            receivingParams.Add(recip);
          }
          outputParams.Add(new OutputParam(ObjectIndex, receivingParams));  //put output param index and recipients into an object in a list
        }
      }
    }

    foreach(IGH_DocumentObject delobj in deletions)  //delete the stuff
    {
      GrasshopperDocument.RemoveObject(delobj, false);
    }

    List<Grasshopper.Kernel.Parameters.Param_Integer> targetParam = new List<Grasshopper.Kernel.Parameters.Param_Integer>();  //holds the new output params as we build them  //rename targetParam

    for (int index = 0; index < _n.Count; index++) {  //this loop runs once per slider bank    //rename index

      targetParam.Add(new Grasshopper.Kernel.Parameters.Param_Integer());  //create the new output param
      targetParam[index].NickName = _controlComponentName + "slids" + index;  //assign the name to the output param including the index number
      GrasshopperDocument.AddObject(targetParam[index], false);

      if(index == 0) {  //put param in place
        targetParam[index].Attributes.Pivot = new System.Drawing.PointF(Component.Attributes.Pivot.X + 20, Component.Attributes.Pivot.Y + 110);
      }
      else
      {
        _n[index] = _n[index] + _n[index - 1];  //aggregate list of number of sliders per bank to create slider index breakpoints
        targetParam[index].Attributes.Pivot = new System.Drawing.PointF(Component.Attributes.Pivot.X + 20, Component.Attributes.Pivot.Y + 110 + _n[index - 1] * 20 + index * 10);
      }

      if (outputParams.Exists(opar => opar.outParam == index))  //looks in the list of deleted output params and determines if one has the same index as the param being created
      {
        foreach(IGH_Param receivingParam in outputParams.Find(opar => opar.outParam == index).recvParams)
        {
          receivingParam.AddSource(targetParam[index]);  //connects the new param to the old param stuff
        }
      }
    }

    Grasshopper.Kernel.Parameters.Param_Point pointsParam = new Grasshopper.Kernel.Parameters.Param_Point();
    pointsParam.NickName = _controlComponentName + "points";
    GrasshopperDocument.AddObject(pointsParam, false);
    pointsParam.Attributes.Pivot = new System.Drawing.PointF(Component.Attributes.Pivot.X + 20, Component.Attributes.Pivot.Y + 70);
    foreach(IGH_Param receivingParam in PointRecvParams)
    {
      receivingParam.AddSource(pointsParam);
    }

    pointsParam.SetPersistentData(_pointsdata.ToArray());



    int Yoffset = -12;
    int CurrentParam = 0;

    for(int i = 0;i < _n[_n.Count - 1];i++)
    {
      if (_n.Exists(x => x == i))
      {
        Yoffset = Yoffset + 10;
        CurrentParam++;
      }
      //instantiate  new slider
      Grasshopper.Kernel.Special.GH_NumberSlider slid = new Grasshopper.Kernel.Special.GH_NumberSlider();
      slid.CreateAttributes(); //sets up default values, and makes sure your slider doesn't crash rhino

      //customise slider (position, ranges etc)
      //targetParam.Attributes.Bounds


      slid.Attributes.Pivot = new System.Drawing.PointF((float) targetParam[0].Attributes.Pivot.X - slid.Attributes.Bounds.Width - 70, (float) targetParam[0].Attributes.Pivot.Y + i * 20 + Yoffset);
      slid.Slider.Maximum = 100;
      slid.Slider.Minimum = 0;
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
      targetParam[CurrentParam].AddSource(slid);
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

    string path = default(string);
    if (inputs[1] != null)
    {
      path = (string)(inputs[1]);
    }

    string controlComponentName = default(string);
    if (inputs[2] != null)
    {
      controlComponentName = (string)(inputs[2]);
    }



    //3. Declare output parameters
      object A = null;


    //4. Invoke RunScript
    RunScript(reset, path, controlComponentName, ref A);
      
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