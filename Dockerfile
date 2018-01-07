FROM mono:5.2

RUN mkdir -p /usr/src/app/source/PingTest && \
  mkdir -p /usr/src/app/build

COPY . /usr/src/app/source/PingTest
WORKDIR /usr/src/app/source/PingTest

# Build the applications
RUN nuget restore -NonInteractive && \
  msbuild PingTest.sln && \
  nuget restore -NonInteractive && \
  cp -rf /usr/src/app/source/PingTest/PingClient/bin/Debug/* /usr/src/app/build && \ 
  cp -rf /usr/src/app/source/PingTest/PingServer/bin/Debug/* /usr/src/app/build && \
  rm -rf packages && \
  msbuild PingTest.sln /target:Clean

EXPOSE 5000

WORKDIR /usr/src/app/build

ENTRYPOINT ["/usr/bin/mono"]
