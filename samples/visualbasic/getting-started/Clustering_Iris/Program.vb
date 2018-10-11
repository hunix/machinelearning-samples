﻿Imports System.IO
Imports Microsoft.ML.Legacy
Imports Microsoft.ML.Legacy.Data
Imports Microsoft.ML.Legacy.Trainers
Imports Microsoft.ML.Legacy.Transforms

Public Module Program
    Private ReadOnly Property AppPath As String
        Get
            Return Path.GetDirectoryName(Environment.GetCommandLineArgs(0))
        End Get
    End Property
    Private ReadOnly Property DataPath As String
        Get
            Return Path.Combine(AppPath, "datasets", "iris-full.txt")
        End Get
    End Property
    Private ReadOnly Property ModelPath As String
        Get
            Return Path.Combine(AppPath, "IrisClustersModel.zip")
        End Get
    End Property

    Sub Main(args() As String)
        MainAsync(args).Wait()
    End Sub

    Public Async Function MainAsync(args() As String) As Task
        ' STEP 1: Create a model
        Dim model = Await TrainAsync()

        ' STEP 2: Make a prediction
        Console.WriteLine()
        Dim prediction1 = model.Predict(TestIrisData.Setosa1)
        Dim prediction2 = model.Predict(TestIrisData.Setosa2)
        Console.WriteLine($"Clusters assigned for setosa flowers:")
        Console.WriteLine($"                                        {prediction1.SelectedClusterId}")
        Console.WriteLine($"                                        {prediction2.SelectedClusterId}")

        Dim prediction3 = model.Predict(TestIrisData.Virginica1)
        Dim prediction4 = model.Predict(TestIrisData.Virginica2)
        Console.WriteLine($"Clusters assigned for virginica flowers:")
        Console.WriteLine($"                                        {prediction3.SelectedClusterId}")
        Console.WriteLine($"                                        {prediction4.SelectedClusterId}")

        Dim prediction5 = model.Predict(TestIrisData.Versicolor1)
        Dim prediction6 = model.Predict(TestIrisData.Versicolor2)
        Console.WriteLine($"Clusters assigned for versicolor flowers:")
        Console.WriteLine($"                                        {prediction5.SelectedClusterId}")
        Console.WriteLine($"                                        {prediction6.SelectedClusterId}")
        Console.ReadLine()
    End Function

    Friend Async Function TrainAsync() As Task(Of PredictionModel(Of IrisData, ClusterPrediction))
        ' LearningPipeline holds all steps of the learning process: data, transforms, learners.
        ' The TextLoader loads a dataset. The schema of the dataset is specified by passing a class containing
        ' all the column names and their types.
        ' ColumnConcatenator concatenates all columns into Features column
        ' KMeansPlusPlusClusterer is an algorithm that will be used to build clusters. We set the number of clusters to 3.
        Dim pipeline = New LearningPipeline From {
             (New TextLoader(DataPath)).CreateFrom(Of IrisData)(useHeader:=True),
             New ColumnConcatenator("Features", "SepalLength", "SepalWidth", "PetalLength", "PetalWidth"),
             New KMeansPlusPlusClusterer With {.K = 3}
        }

        Console.WriteLine("=============== Training model ===============")
        Dim model = pipeline.Train(Of IrisData, ClusterPrediction)()
        Console.WriteLine("=============== End training ===============")

        ' Saving the model as a .zip file.
        Await model.WriteAsync(ModelPath)
        Console.WriteLine("The model is saved to {0}", ModelPath)

        Return model
    End Function
End Module
