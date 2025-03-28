load('ext://helm_resource', 'helm_resource', 'helm_repo')
helm_repo('stackgres', 'https://stackgres.io/downloads/stackgres-k8s/stackgres/helm/')
helm_resource('stackgres-charts', resource_deps=['stackgres'])