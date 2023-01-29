warning("Please manually run the code line by line and save the image from the preview box.")
source("00.Functions.R")
CheckPackage(c("survival"))

# We need some output from the nomogram
source("04.Nomogram.R")

cox <- coxph(Surv(Time,Status) ~ Age+Gender+M+T+`TIL(%)`, data = data.nomo)
data.roc = data.frame(
  Time = data.nomo$Time,
  Status = data.nomo$Status,
  Factor = predict(cox, type = "lp")
)
TimeROC(data = data.roc)

# XJH
data.nomo.XJH = read_excel("Data/OrginalData.XJH.xlsx")
data.nomo.XJH$Age = ifelse(data.nomo.XJH$Age < 65, "<65", ">=65")
ret = c("Age", "Gender", "M", "T", "prec.TIL", "Time", "Status")
data.nomo.XJH = data.nomo.XJH[,colnames(data.nomo.XJH) %in% ret]

i = 0
for (variable in colnames(data.nomo.XJH)) {
  i = i + 1
  if (colnames(data.nomo.XJH)[i] == "prec.TIL") {
    colnames(data.nomo.XJH)[i] = "TIL(%)"
    break
  }
}

cox.XJH <- coxph(Surv(Time,Status) ~ Age+Gender+M+T+`TIL(%)`, data = data.nomo.XJH)
data.roc.XJH = data.frame(
  Time = data.nomo.XJH$Time,
  Status = data.nomo.XJH$Status,
  Factor = predict(cox.XJH, type = "lp")
)
TimeROC(data = data.roc.XJH)