using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace Dhara
{
    public sealed partial class EnvironmentalDashboard
    {
        void OpenSensitivity()
        {
            if(Comparison==null)CaptureStudio();if(Comparison==null)return;var frozen=Comparison;
            var panel=Tool("ENVIRONMENTAL SENSITIVITY ANALYSIS");
            Text(panel,"Fixed conditions",frozen.Location+" · "+frozen.Classification+" · "+(frozen.Input.Time??"synthetic time")+"\nFixed inputs: "+frozen.Input.RainMmPerHour.Value.ToString("0.0")+" mm/h rain, "+frozen.Drainage.ToString("0.0")+" mm/h nominal drainage. Selected interventions held fixed except the chosen variable. Every case starts dry for 60 minutes.",14,.03f,.72f,.94f,.16f,muted);
            SensitivityVariable variable=SensitivityVariable.RainMmHour;
            var selector=Button(panel,"Variable: rain mm/h",.03f,.62f,.94f,.08f,()=>{});
            var min=Slider(panel,"MINIMUM",0,200,.03f,.47f,.29f,.13f,v=>{},"");
            var max=Slider(panel,"MAXIMUM",0,200,.35f,.47f,.29f,.13f,v=>{},"");max.SetValueWithoutNotify(100);
            var step=Slider(panel,"STEP SIZE",1,100,.67f,.47f,.30f,.13f,v=>{},"");step.SetValueWithoutNotify(20);
            selector.onClick.AddListener(()=>{variable=(SensitivityVariable)(((int)variable+1)%4);selector.GetComponentInChildren<TMP_Text>().text="Variable: "+variable;min.maxValue=max.maxValue=variable==SensitivityVariable.RainMmHour?200:100;});
            foreach(var update in sliderLabels)update();
            var table=Text(panel,"Calculated cases","Set the range and step, then evaluate. At most 21 cases.\nOutputs are model sensitivity, not evidence of real-world causal magnitude.",15,.03f,.12f,.94f,.31f,ink);
            var rows=new List<SensitivityCase>();int page=0;
            Action draw=()=>{table.text="VALUE → peak runoff m³/h / max collection m³ / infiltrated m³\n";for(int i=page*6;i<Math.Min(rows.Count,page*6+6);i++){var c=rows[i];table.text+=$"{c.Value:0.0} → {c.Result.PeakRunoffM3Hour:0.000} / {c.Result.MaximumStorageM3:0.000} / {c.Result.InfiltratedM3:0.000}\n";}table.text+=$"{rows.Count} calculated cases · page {page+1}";};
            Button(panel,"Evaluate range",.03f,.025f,.46f,.08f,()=>{try{rows=SensitivityAnalysis.Evaluate(frozen.Input.RainMmPerHour.Value,frozen.Drainage,frozen.Configuration,variable,min.value,max.value,step.value);page=0;draw();}catch(Exception e){table.text=e.Message;}});
            Button(panel,"Previous",.51f,.025f,.22f,.08f,()=>{page=Math.Max(0,page-1);draw();});
            Button(panel,"Next",.75f,.025f,.22f,.08f,()=>{page=Math.Min(Math.Max(0,(rows.Count-1)/6),page+1);draw();});
        }
    }
}
