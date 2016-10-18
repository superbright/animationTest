#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Slate{

	///A DopeSheet editor for animation curves
	public static class DopeSheetEditor{

		private static Dictionary<IAnimatableData, DopeSheetRenderer> cache = new Dictionary<IAnimatableData, DopeSheetRenderer>();
		public static void DrawDopeSheet(IAnimatableData animatable, IKeyable keyable, Rect rect, float startTime, float maxTime, bool highlightRange = true){
			DopeSheetRenderer dopeSheet = null;
			if (!cache.TryGetValue(animatable, out dopeSheet)){
				cache[animatable] = dopeSheet = new DopeSheetRenderer();
			}
			dopeSheet.DrawDopeSheet(animatable, keyable, rect, startTime, maxTime, highlightRange);
		}


		class DopeSheetRenderer {

			const float PROXIMITY_TOLERANCE = 0.001f;

			private AnimationCurve[] allCurves;
			private float maxTime;
			private Rect rect;
			private float width;
			private float startTime;

			private int pickIndex = -1;
			private List<float> currentTimes;
			private List<float> prePickTimes;

			private Keyframe[] copyKeyframes;

			private float? selectionStartPos; //used only for creating the rect
			private float? startDragTime; //the time which we started dragging the selection rect
			private Rect? timeSelectionRect; //the actual selection rect
			private Rect preScaleSelectionRect; //the rect before start retiming selection rect
			private bool isRetiming; //is retiming/scaling keys?
			private List<int> rectSelectedIndeces = new List<int>(); //the indeces of dopekeys that were originaly in the selection rect
			private Rect pixelSelectionRect{ //the converted selection rect from time to pos/pixels
				get
				{
					if (timeSelectionRect == null) return new Rect();
					var temp = timeSelectionRect.Value;
					return Rect.MinMaxRect( TimeToPos(temp.xMin), temp.yMin, TimeToPos(temp.xMax), temp.yMax );
				}
			}


			float TimeToPos(float time){
				return ( (time - startTime) / maxTime) * width + rect.x;
			}

			float PosToTime(float pos){
				return ((pos - rect.x) / width) * maxTime + startTime;
			}

			public void DrawDopeSheet( IAnimatableData animatable, IKeyable keyable, Rect rect, float startTime, float maxTime, bool highlightRange){

				var e          = Event.current;
				this.maxTime   = maxTime;
				this.rect      = rect;
				this.width     = rect.width;
				this.startTime = startTime;
				this.allCurves = animatable.GetCurves();

				if (allCurves == null || allCurves.Length == 0){
					GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height), "NO CURVES");
					return;
				}

				//get all curve keys
				var keys = new List<Keyframe>();
				for (var i = 0; i < allCurves.Length; i++){
					keys.AddRange(allCurves[i].keys);
				}
				keys = keys.OrderBy(k => k.time).ToList();

				///Range graphics
				if (highlightRange){
					var firstKeyPos = TimeToPos( keys.FirstOrDefault().time );
					var lastKeyPos = TimeToPos( keys.LastOrDefault().time );
					if (Mathf.Abs(firstKeyPos - lastKeyPos) > 0){
						var rangeRect = Rect.MinMaxRect( firstKeyPos - 8, rect.yMin, lastKeyPos + 8, rect.yMax );
						rangeRect.xMin = Mathf.Max(rangeRect.xMin, rect.xMin);
						rangeRect.xMax = Mathf.Min(rangeRect.xMax, rect.xMax);
						GUI.color = new Color(0f,0.5f,0.5f,0.4f);
						GUI.Box(rangeRect, "", Slate.Styles.clipBoxStyle);
						GUI.color = Color.white;
					}
				}

				//bg graphics
				GUI.color = new Color(0,0,0,0.1f);
				var center = rect.y + (rect.height/2);
				var lineRect = Rect.MinMaxRect( rect.x, center - 1, rect.xMax, center + 1);
				GUI.DrawTexture(lineRect, Slate.Styles.whiteTexture);
				GUI.color = Color.white;

				//create dope keys when needed
				if (pickIndex == -1 && timeSelectionRect == null){
					currentTimes = new List<float>();
					foreach (var key in keys){
						if ( !currentTimes.Any( t => Mathf.Abs(t - key.time) <= PROXIMITY_TOLERANCE )){
							currentTimes.Add(key.time);
						}
					}
				}

				if (timeSelectionRect != null){
					GUI.Box(pixelSelectionRect, "");
					GUI.color = new Color(0.5f,0.5f,1, 0.25f);
					GUI.DrawTexture(pixelSelectionRect, Slate.Styles.whiteTexture);
					GUI.color = Color.white;
				}


				//draw the dopekeys
				for (var t = 0; t < currentTimes.Count; t++){
					var time = currentTimes[t];

					//hide if out of view range (+- some extra offset)
					if (time < startTime - 1 || time > startTime + maxTime + 1 ){
						continue;
					}

					//graphics
					var icon = Slate.Styles.dopeKey;
					if (Prefs.keyframesStyle == Prefs.KeyframesStyle.PerTangentMode){
						var checkTime = pickIndex == -1? currentTimes[t] : prePickTimes[t];
						var resultTangentMode = TangentMode.Editable;
						var first = true;
						foreach(var key in keys.Where( k => Mathf.Abs(k.time - checkTime) <= PROXIMITY_TOLERANCE )){
							var keyTangent = KeyframeUtility.GetKeyTangentMode(key, 1);
							if (first){
								resultTangentMode = keyTangent;
								first = false;
								continue;
							}
							if (keyTangent != resultTangentMode){
								resultTangentMode = TangentMode.Editable;
								break;
							}
						}
						if (resultTangentMode != TangentMode.Editable){
							if (resultTangentMode == TangentMode.Smooth){
								icon = Slate.Styles.dopeKeySmooth;
							}
							if (resultTangentMode == TangentMode.Constant){
								icon = Slate.Styles.dopeKeyConstant;
							}
							if (resultTangentMode == TangentMode.Linear){
								icon = Slate.Styles.dopeKeyLinear;
							}
						}
					}

					var dopeKeyRect = new Rect(0,0,icon.width,icon.height);
					dopeKeyRect.center = new Vector2( TimeToPos(time), rect.center.y );
					var isSelected = t == pickIndex || rectSelectedIndeces.Contains(t);
					GUI.color = isSelected? new Color(0.6f,0.6f,1) : Color.white;
					GUI.DrawTexture(dopeKeyRect, icon);
					GUI.color = Color.white;


					if (Prefs.showDopesheetKeyValues){
						var nextPos = t < currentTimes.Count-1? TimeToPos(currentTimes[t + 1]) : TimeToPos(maxTime);
						var valueRect = Rect.MinMaxRect(dopeKeyRect.xMax, rect.yMin-3, nextPos - dopeKeyRect.width/2, rect.yMax);
						var evalTime = pickIndex == -1? currentTimes[t] : prePickTimes[t];
						GUI.Label(valueRect, string.Format("<size=8>{0}</size>", animatable.GetKeyLabel(evalTime)), Slate.Styles.leftLabel);
					}


					//do the following only if we dont have a rect selection
					if (timeSelectionRect == null){

						//pick the key
						if (e.type == EventType.MouseDown && dopeKeyRect.Contains(e.mousePosition)){
							prePickTimes = new List<float>(currentTimes);
							pickIndex = t;
							if (e.clickCount == 2){
								keyable.root.currentTime = time + keyable.startTime;
								CutsceneUtility.selectedObject = keyable;
							}
							e.Use();
							break;
						}

						//single key context menu
						if (e.type == EventType.MouseUp && e.button == 1 && dopeKeyRect.Contains(e.mousePosition)){

							var menu = new GenericMenu();
							menu.AddItem(new GUIContent("Jump Here (Double Click)"), false, ()=> { keyable.root.currentTime = time + keyable.startTime; });
							menu.AddItem(new GUIContent("Smooth"), false, ()=> { SetKeyTangentMode(time, TangentMode.Smooth); });
							menu.AddItem(new GUIContent("Linear"), false, ()=> { SetKeyTangentMode(time, TangentMode.Linear); });
							menu.AddItem(new GUIContent("Constant"), false, ()=> { SetKeyTangentMode(time, TangentMode.Constant); });
							menu.AddItem(new GUIContent("Editable"), false, ()=> { SetKeyTangentMode(time, TangentMode.Editable); });

							menu.AddItem(new GUIContent("Copy Key"), false, ()=>
								{
									copyKeyframes = new Keyframe[allCurves.Length];
									for (var i = 0; i < allCurves.Length; i++){
										var c = allCurves[i];
										for (var j = 0; j < c.keys.Length; j++){
											var key = c.keys[j];
											if ( Mathf.Abs(key.time - time) <= PROXIMITY_TOLERANCE ){
												copyKeyframes[i] = key;
											}
										}
									}							
								});

							menu.AddSeparator("/");
							menu.AddItem(new GUIContent("Delete Key"), false, ()=>
								{
									foreach(var c in allCurves){
										for (int i = 0; i < c.keys.Length; i++){
											var key = c.keys[i];
											if ( Mathf.Abs(key.time - time) <= PROXIMITY_TOLERANCE ){
												c.RemoveKey(i);
											}
										}
										c.UpdateTangentsFromMode();
									}
								});
							
							menu.ShowAsContext();
							pickIndex = -1;
							e.Use();
							break;
						}
					}
				}

				//SINGLE/NO KEY
				if (timeSelectionRect == null){

					//create new key at time context menu
					if (e.type == EventType.MouseUp && e.button == 1 && rect.Contains(e.mousePosition)){
						
						var cursorTime = PosToTime(e.mousePosition.x);
						var menu = new GenericMenu();

						menu.AddItem(new GUIContent("Create Key"), false, ()=> { animatable.TryKeyIdentity(keyable.animatedParametersTarget, cursorTime); });

						if (copyKeyframes != null && copyKeyframes.Length == allCurves.Length){
							menu.AddItem(new GUIContent("Paste Key"), false, ()=>
							{
								for (var i = 0; i < allCurves.Length; i++){
									var c = allCurves[i];
									var key = copyKeyframes[i];
									key.time = cursorTime;
									var index = c.AddKey(key);
									c.MoveKey(index, key);
									c.UpdateTangentsFromMode();
								}	
							});
						}

						menu.ShowAsContext();
						e.Use();
					}

					//drag the picked key if any. Shift drags all next to it as well
					if (pickIndex != -1 && e.type == EventType.MouseDrag && e.button == 0){
						var lastTime = currentTimes[pickIndex];
						var newTime = PosToTime(e.mousePosition.x);
						newTime = Mathf.Round(newTime / Prefs.snapInterval) * Prefs.snapInterval;
						newTime = Mathf.Clamp(newTime, startTime, startTime + maxTime);
						if (e.shift){
							var max = pickIndex > 0? currentTimes[pickIndex-1] + Prefs.snapInterval : startTime;
							newTime = Mathf.Max(newTime, max);
							foreach(var time in currentTimes.Where(k => k > lastTime)){
								var index = currentTimes.IndexOf(time);
								currentTimes[index] += newTime - lastTime;
							}
						}
						currentTimes[pickIndex] = newTime;
					}

					//apply the changes when mouse up and deselect key
					if (pickIndex != -1 && e.rawType == EventType.MouseUp){
						pickIndex = -1;
						Apply();
					}
				}


				//SELECTION RECT DRAG. TODO: Heavy refactor!
				if (pickIndex == -1){

					if (e.rawType == EventType.MouseDown){

						//if no rect selection, start one.
						if (timeSelectionRect == null && e.button == 0){
							if (rect.Contains(e.mousePosition)){
								selectionStartPos = e.mousePosition.x;
							}

						} else {

							//if we have a rect and mouse contains it, initialize original values and keys.
							if (pixelSelectionRect.Contains(e.mousePosition)){
								prePickTimes = new List<float>(currentTimes);
								startDragTime = (float)PosToTime(e.mousePosition.x);
								preScaleSelectionRect = timeSelectionRect.Value;
								rectSelectedIndeces = new List<int>();
								isRetiming = false;

								var temp = timeSelectionRect.Value;
								for (var i = 0; i < currentTimes.Count; i++){
									if (currentTimes[i] >= temp.xMin && currentTimes[i] <= temp.xMax){
										rectSelectedIndeces.Add(i);
									}
								}

							//if we have a rect, but mouse is outside, clear all and reset values.
							} else {
								timeSelectionRect = null;
								selectionStartPos = null;
								rectSelectedIndeces.Clear();
								isRetiming = false;
							}
						}
					}

					//create the selection rect
					if (selectionStartPos != null){
						var a = PosToTime(selectionStartPos.Value);
						var b = PosToTime(e.mousePosition.x);
						// a = Mathf.Round(a / Prefs.snapInterval) * Prefs.snapInterval;
						// b = Mathf.Round(b / Prefs.snapInterval) * Prefs.snapInterval;

						if (Mathf.Abs(a - b) >= 0.001f){
							timeSelectionRect = Rect.MinMaxRect( Mathf.Min(a, b), rect.yMin, Mathf.Max(a, b), rect.yMax );
						} else {
							timeSelectionRect = null;
						}
					}

					//move the rect, or scale/retime it along with it's contained keys
					if (timeSelectionRect != null){
						var temp = timeSelectionRect.Value;
						var retimeInRect = Rect.MinMaxRect(pixelSelectionRect.xMin, pixelSelectionRect.yMin, pixelSelectionRect.xMin+4, pixelSelectionRect.yMax);
						var retimeOutRect = Rect.MinMaxRect(pixelSelectionRect.xMax-4, pixelSelectionRect.yMin, pixelSelectionRect.xMax, pixelSelectionRect.yMax);
						EditorGUIUtility.AddCursorRect(retimeInRect, MouseCursor.ResizeHorizontal);
						EditorGUIUtility.AddCursorRect(retimeOutRect, MouseCursor.ResizeHorizontal);
						EditorGUIUtility.AddCursorRect(pixelSelectionRect, MouseCursor.Link);
						GUI.Box(retimeInRect, "");
						GUI.Box(retimeOutRect, "");
						//set retiming on mouse down and containing edge rects
						if (e.type == EventType.MouseDown && e.button == 0 && (retimeInRect.Contains(e.mousePosition) || retimeOutRect.Contains(e.mousePosition)) ){
							isRetiming = true;
						}

						if (e.type == EventType.MouseDrag && e.button == 0){

							var pointerTime = PosToTime(e.mousePosition.x);
							
							if (isRetiming){

								//retime from either start or end, whichever closer. This allows for negative retime/scale
								var retimeIn = Mathf.Abs(pointerTime - temp.x) < Mathf.Abs(pointerTime - temp.xMax);
								if (retimeIn){ temp.xMin = Mathf.Max(pointerTime, 0);}
								else { temp.xMax = pointerTime; }
								
								foreach(var index in rectSelectedIndeces){
									var preTime = prePickTimes[index];
									var norm = Mathf.InverseLerp(preScaleSelectionRect.xMin, preScaleSelectionRect.xMax, preTime);
									currentTimes[index] = Mathf.Lerp(temp.xMin, temp.xMax, norm);
								}

							} else {

								if (startDragTime != null){
									var delta = pointerTime - (float)startDragTime;
									if (temp.x + delta >= 0){
										foreach(var index in rectSelectedIndeces){
											currentTimes[index] += delta;
										}

										temp.x += delta;
										startDragTime = (float)pointerTime;
									}
								}
							}
						}

					
						//re-apply timeselection rect from working temp
						timeSelectionRect = temp;
					}

					//Apply all changes and reset values on MouseUp
					if (e.rawType == EventType.MouseUp){
						if (timeSelectionRect != null && (startDragTime != null || isRetiming) ){
							Apply();
						}

						selectionStartPos = null;
						startDragTime = null;
						isRetiming = false;
					}

					//rect selection context menu
					if (timeSelectionRect != null){
						if (e.type == EventType.MouseUp && e.button == 1 && pixelSelectionRect.Contains(e.mousePosition)){
							var menu = new GenericMenu();
							menu.AddItem(new GUIContent("Smooth Keys"), false, ()=> { SetSelectionTangentMode(TangentMode.Smooth); });
							menu.AddItem(new GUIContent("Linear Keys"), false, ()=> { SetSelectionTangentMode(TangentMode.Linear); });
							menu.AddItem(new GUIContent("Constant Keys"), false, ()=> { SetSelectionTangentMode(TangentMode.Constant); });
							menu.AddItem(new GUIContent("Editable Keys"), false, ()=> { SetSelectionTangentMode(TangentMode.Editable); });
							menu.AddSeparator("/");
							menu.AddItem(new GUIContent("Delete Keys"), false, ()=>
							{
								foreach(var index in rectSelectedIndeces){
									foreach(var curve in allCurves){
										for (var i = 0; i < curve.keys.Length; i++){
											var key = curve[i];
											if ( Mathf.Abs(key.time - currentTimes[index]) <= PROXIMITY_TOLERANCE ){
												curve.RemoveKey(i);
											}
										}
										curve.UpdateTangentsFromMode();
									}
								}

								timeSelectionRect = null;
								rectSelectedIndeces.Clear();
							});

							menu.ShowAsContext();
							e.Use();
						}
					}
				}
				///


				//use mouse events so that dont pass through
				if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition)){
					e.Use();
				}
			}


			///Set key tangent mode at time
			void SetKeyTangentMode(float time, TangentMode mode){
				foreach(var c in allCurves){
					for (int i = 0; i < c.keys.Length; i++){
						var key = c.keys[i];
						if ( Mathf.Abs(key.time - time) <= PROXIMITY_TOLERANCE ){
							key = KeyframeUtility.GetNewModeFromExistingKey(key, mode);
							c.MoveKey(i, key);
							c.UpdateTangentsFromMode();
						}
					}
				}				
			}

			///Set all selected keys tangent mode
			void SetSelectionTangentMode(TangentMode mode){
				foreach(var index in rectSelectedIndeces){
					foreach(var curve in allCurves){
						for (var i = 0; i < curve.keys.Length; i++){
							var key = curve[i];
							if ( Mathf.Abs(key.time - currentTimes[index]) <= PROXIMITY_TOLERANCE ){
								key = KeyframeUtility.GetNewModeFromExistingKey(key, mode);
								curve.MoveKey(i, key);
							}
						}
						curve.UpdateTangentsFromMode();
					}
				}				
			}


			//apply the changed key times
			void Apply(){
				
				for (var i = 0; i < prePickTimes.Count; i++){

					if ( Mathf.Abs(prePickTimes[i] - currentTimes[i]) >= float.Epsilon ){

						var lastTime = prePickTimes[i];
						var newTime = currentTimes[i];
						
						foreach(var curve in allCurves){

							for (var j = 0; j < curve.keys.Length; j++){
								var key = curve.keys[j];
								if ( Mathf.Abs(key.time - lastTime) <= PROXIMITY_TOLERANCE ){
									key.time = newTime;
									curve.MoveKey(j, key);
									break;
								}
							}

							curve.UpdateTangentsFromMode();
						}
					}
				}
			}


		}
	}
}

#endif