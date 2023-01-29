warning("Please manually run the code line by line and save the image from the preview box.")
source("00.Functions.R")
CheckPackage(c("readxl"))

# First we need to get the cutoff value
source("01.CalculateCutoff.R")
data.cox = OrginalData.TCGA
data.cox$Status = ifelse(data.cox$Time > 5, 0, as.numeric(data.cox$Status))
data.cox$Time = ifelse(data.cox$Time > 5, 5 ,as.numeric(data.cox$Time))
data.cox$Name = NULL
data.cox$Group = NULL
data.cox$NewTumorEvent = data.cox$NewTumorTime = NULL
data.cox$TIL = ifelse(data.cox$prec.TIL > res.cut.TCGA$prec.TIL$estimate, "High TIL", "Low TIL")
data.cox$TLS = ifelse(data.cox$prec.TLS > 0, "TLS", "Non-TLS")
data.cox$prec.TLS = data.cox$prec.TIL = NULL
data.cox$Age = ifelse(data.cox$Age < 65, "<65", ">=65")
data.cox$M = ifelse(data.cox$M == "M0" , "M0", "M1 / Mx")
data.cox$N = ifelse(data.cox$N == "N0" , "N0", "N1 / Nx")
data.cox$T = ifelse(data.cox$T == "T1" | data.cox$T == "T2" , "T1 - T2", "T3 - T4")
data.cox$Stage = NULL
result.cox = Cox(data.cox, type = "mix", plot = T)