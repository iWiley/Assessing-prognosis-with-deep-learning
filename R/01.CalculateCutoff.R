warning("Please manually run the code line by line and save the image from the preview box.")
source("00.Functions.R")
CheckPackage(c("readxl", "survminer"))

# Cutoff
OrginalData.TCGA <- read_excel("Data/OrginalData.TCGA.xlsx")
OrginalData.XJH = read_excel("Data/OrginalData.XJH.xlsx")
res.cut.TCGA <- surv_cutpoint(OrginalData.TCGA,
                         time = "Time",
                         event = "Status",
                         variables = "prec.TIL"
)
res.cut.XJH <- surv_cutpoint(OrginalData.XJH,
                         time = "Time",
                         event = "Status",
                         variables = "prec.TIL"
)
summary(res.cut.TCGA)
summary(res.cut.XJH)
plot(res.cut.TCGA, "prec.TIL", palette = "npg")
# Draw using the cutoff value of the XJH cohort
plot(res.cut.XJH, "prec.TIL", palette = "npg")
# Draw using the cutoff value of the TCGA cohort
res.cut.XJH$prec.TIL$estimate = res.cut.TCGA$prec.TIL$estimate
plot(res.cut.XJH, "prec.TIL", palette = "npg")