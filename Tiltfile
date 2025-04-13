load('ext://helm_resource', 'helm_resource', 'helm_repo')
helm_repo('stackgres-charts', 'https://stackgres.io/downloads/stackgres-k8s/stackgres/helm/')

k8s_yaml('./k8s/local-namespace.yaml')

helm_resource(
    name='stackgres-operator',
    chart='stackgres-charts/stackgres-operator',
    resource_deps=[
        'stackgres-charts'
    ],
    namespace='stackgres',
    flags=[
        '--wait',
        '--version=1.15.2'
    ],
)

k8s_yaml('./k8s/local-postgres.yaml')