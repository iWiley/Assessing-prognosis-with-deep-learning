## Introduction

This repository contains the source code and data of all programs used in the manuscript "Potential application of deep learning for assessing immune infiltration in hepatocellular carcinoma".

## Folder Structure

    DL-in-HCC
    ├── C#                                            C# related projects
    │   ├── Deep Learning Training Command Line Tool    Function as folder name
    │   ├── DL-in-HCC.sln                               Solution file
    │   ├── Tile Classifier                             Function as folder name
    │   └── Wistu.Lib.ClassifyModel                     Public library, 'wistu' is the author's blog name
    ├── LICENSE
    ├── R                                             R related scripts
    │   ├── 00.Functions.R                              Basic functions of the script
    │   ├── 01.CalculateCutoff.R                        The name is its function
    │   ├── 02.DrawOS.R                                 The name is its function
    │   ├── 03.CoxRegression.R                          The name is its function
    │   ├── 04.Nomogram.R                               The name is its function
    │   ├── 05.CalibrationCurve.R                       The name is its function
    │   ├── 06.Time-dependentROCCurve.R                 The name is its function
    │   ├── 07.CIBERSORT.R                              The name is its function
    │   ├── 08.EnrichmentAnalysis.R                     The name is its function
    │   ├── 09.ImmuneCheckpointExpression.R             The name is its function
    │   ├── Data                                        Contains the data needed for the script
    │   └── R.Rproj                                     R project file
    └── README.md

## C# Projects

### Deep Learning Training Command Line Tool

This tool is used for model training, validation, etc. It also contains some miscellaneous functions such as segmenting WSI, viewing WSI information, calculating immune scores, etc.

#### Usage

    Usage:
      DeepLearningTrainingCommandLineTool [command] [options]

    Options:
      --version       Show version information
      -?, -h, --help  Show help and usage information

    Commands:
      cut       Automatically cut the entire WSI file.
      view      View the specified location in the WSI file.
      info      View WSI file information.
      grey      Image graying.
      verify    Validate the results against the model.
      cpstruct  Copy the structure of the input folder according to the specified directory and save it to the output
                folder.
      train     Start training.
      models    Show all models.
      link      Create hard links for files in the specified directory structure.
      score     Calculates the immune species score for the slice in the specified slice catalog.

#### Hardware Requirements

The hardware requirements for this software are basically equivalent to TensorFlow. If you want to use a graphics card for training, you need a corresponding graphics card, such as a discrete graphics card that supports NVIDIA cuda.

#### Obtaining training data in manuscripts

The training data has been packaged in 7zip format in 2000M sub-packages, but the authors were unable to place these files in the repository due to the file size limitations of the Github repository. As an alternative, we have placed these files on the release page, and you can go to get them.

#### How to train a model

To train the model you need to create subdirectories in the training directory according to the total number of classifications of your model, as required by ML.NET. After that place your files there by type and run the train command for training.

### Tile Classifier

The main role of this tool is to assist users in fast and efficient tile classification, while it can use already trained models to assist in classification.

#### Usage

Use keyboard shortcuts to sort the tiles. For more details, please refer to the source code.

## R Projects
The R scripts have been split into separate units according to their functions, and each R script runs interdependently but independently of each other. Note: If you cannot output the image by executing the corresponding R script directly, please press Ctrl+Enter in R studio to run the code line by line and get the image in the Plot window.

## How to get the original data？
The raw data of R scripts are stored in the /R/Data directory, and the deep learning training data used by C# programs are available on the release page. In addition, the tile data of the Xijing Hospital cohort used for validation is stored in the RELEASE page.