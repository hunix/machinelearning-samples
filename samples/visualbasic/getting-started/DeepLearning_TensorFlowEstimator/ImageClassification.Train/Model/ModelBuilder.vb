﻿Imports System
Imports System.IO
Imports System.Linq
Imports ImageClassification.ImageData
Imports Microsoft.ML.Runtime.Data
Imports Microsoft.ML.Runtime.ImageAnalytics
Imports Microsoft.ML.Transforms
Imports Microsoft.ML.Runtime
Imports Microsoft.ML
Imports Microsoft.ML.Trainers
Imports Microsoft.ML.Runtime.Api
Imports ImageClassification.Model.ConsoleHelpers
Imports Microsoft.ML.Runtime.Learners
Imports Microsoft.ML.Transforms.Categorical
Imports Microsoft.ML.Transforms.Conversions

Namespace ImageClassification.Model
	Public Class ModelBuilder
		Private ReadOnly dataLocation As String
		Private ReadOnly imagesFolder As String
		Private ReadOnly inputModelLocation As String
		Private ReadOnly outputModelLocation As String
		Private ReadOnly env As IHostEnvironment

		Public Sub New(ByVal dataLocation As String, ByVal imagesFolder As String, ByVal inputModelLocation As String, ByVal outputModelLocation As String)
			Me.dataLocation = dataLocation
			Me.imagesFolder = imagesFolder
			Me.inputModelLocation = inputModelLocation
			Me.outputModelLocation = outputModelLocation
			env = New ConsoleEnvironment(seed:= 1)
		End Sub

		Private Structure ImageNetSettings
			Public Const imageHeight As Integer = 224
			Public Const imageWidth As Integer = 224
			Public Const mean As Single = 117
			Public Const scale As Single = 1
			Public Const channelsLast As Boolean = True
		End Structure

		Public Sub BuildAndTrain()
			Dim featurizerModelLocation = inputModelLocation

			ConsoleWriteHeader("Read model")
			Console.WriteLine($"Model location: {featurizerModelLocation}")
			Console.WriteLine($"Images folder: {imagesFolder}")
			Console.WriteLine($"Training file: {dataLocation}")
			Console.WriteLine($"Default parameters: image size=({ImageNetSettings.imageWidth},{ImageNetSettings.imageHeight}), image mean: {ImageNetSettings.mean}")



			Dim loader = New TextLoader(env, New TextLoader.Arguments With {
				.Column = {
					New TextLoader.Column("ImagePath", DataKind.Text, 0),
					New TextLoader.Column("Label", DataKind.Text, 1)
				}
			})



			Dim pipeline = (New ValueToKeyMappingEstimator(env, "Label", "LabelTokey")).Append(New ImageLoadingEstimator(env, imagesFolder, ("ImagePath", "ImageReal"))).Append(New ImageResizingEstimator(env, "ImageReal", "ImageReal", ImageNetSettings.imageHeight, ImageNetSettings.imageWidth)).Append(New ImagePixelExtractingEstimator(env, { New ImagePixelExtractorTransform.ColumnInfo("ImageReal", "input", interleave:= ImageNetSettings.channelsLast, offset:= ImageNetSettings.mean) })).Append(New TensorFlowEstimator(env, featurizerModelLocation, { "input" }, { "softmax2_pre_activation" })).Append(New SdcaMultiClassTrainer(env, "softmax2_pre_activation", "LabelTokey")).Append(New KeyToValueEstimator(env, ("PredictedLabel", "PredictedLabelValue")))

			' Train the pipeline
			ConsoleWriteHeader("Training classification model")
			Dim data = loader.Read(New MultiFileSource(dataLocation))
			Dim model = pipeline.Fit(data)

			' Process the training data through the model
			' This is an optional step, but it's useful for debugging issues
			Dim trainData = model.Transform(data)
			Dim loadedModelOutputColumnNames = trainData.Schema.GetColumnNames()
			Dim trainData2 = trainData.AsEnumerable(Of ImageNetPipeline)(env, False, True).ToList()
			trainData2.ForEach(Function(pr) ConsoleWriteImagePrediction(pr.ImagePath,pr.PredictedLabelValue, pr.Score.Max()))

			' Get some performance metric on the model using training data            
			Dim sdcaContext = New MulticlassClassificationContext(env)
			ConsoleWriteHeader("Classification metrics")
			Dim metrics = sdcaContext.Evaluate(trainData, label:= "LabelTokey", predictedLabel:= "PredictedLabel")
			Console.WriteLine($"LogLoss is: {metrics.LogLoss}")
			Console.WriteLine($"PerClassLogLoss is: {String.Join(", ", metrics.PerClassLogLoss.Select(Function(c) c.ToString()))}")

			' Save the model to assets/outputs
			ConsoleWriteHeader("Save model to local file")
			ModelHelpers.DeleteAssets(outputModelLocation)
			Using f = New FileStream(outputModelLocation, FileMode.Create)
				model.SaveTo(env, f)
			End Using
			Console.WriteLine($"Model saved: {outputModelLocation}")
		End Sub

	End Class
End Namespace
